using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace VoidClash
{
    /// <summary>A placed structure. Starts as a construction site (worker must build it),
    /// then provides supply / training / dropoff / turret defense when complete.</summary>
    public class Building : Entity
    {
        public BuildingData Data { get; private set; }
        public bool IsComplete { get; private set; }
        public float BuildProgress { get; private set; }
        public float ApproachRange => Mathf.Max(Data.sizeX, Data.sizeZ) * 0.5f + 1.2f;

        /// <summary>Planar distance from a point to the building's rectangular footprint
        /// (0 when inside). Use this for interaction ranges — center distance fails on
        /// diagonal approaches because navmesh carving expands the blocked area.</summary>
        public float DistanceToEdge(Vector3 p)
        {
            float dx = Mathf.Max(0f, Mathf.Abs(p.x - transform.position.x) - Data.sizeX * 0.5f);
            float dz = Mathf.Max(0f, Mathf.Abs(p.z - transform.position.z) - Data.sizeZ * 0.5f);
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        public override bool IsBuilding => true;
        public override string DisplayName => Data != null ? Data.displayName : name;
        public override ArmorClass ArmorClass => ArmorClass.Structure;

        // training
        public readonly List<UnitData> Queue = new List<UnitData>();
        public float CurrentTrainProgress { get; private set; }
        public Vector3? RallyPoint { get; private set; }
        GameObject _rallyFlag;

        Transform _visual;
        Weapon _weapon;
        Entity _turretTarget;
        float _scanTimer;
        bool _contributedThisFrame;
        ParticleSystem _dust;
        float _supplyGiven;
        NavMeshObstacle _obstacle;

        // ---- Terran-style lift-off ----
        public enum FlightState { Grounded, Lifting, Flying, Landing }
        public FlightState Flight { get; private set; } = FlightState.Grounded;
        public bool CanLift => IsComplete && !IsDead && Data.canLift;
        public bool IsAirborne => Flight != FlightState.Grounded;
        Vector3 _flightDest;
        bool _hasFlightDest;
        Vector3 _landSpot;
        const float FlyHeight = 5.5f;
        const float LiftSpeed = 2.2f;
        const float FlySpeed = 3.2f;

        public const int MaxQueue = 5;

        public void Init(BuildingData data, Faction faction, bool preBuilt)
        {
            Data = data;
            Faction = faction;
            VisionRadius = data.visionRadius;
            Radius = Mathf.Max(data.sizeX, data.sizeZ) * 0.55f;
            gameObject.layer = LayerMask.NameToLayer("Buildings");

            var col = gameObject.GetComponent<BoxCollider>();
            if (col == null) col = gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 1.25f, 0f);
            col.size = new Vector3(data.sizeX, 2.5f, data.sizeZ);

            _obstacle = gameObject.GetComponent<NavMeshObstacle>();
            if (_obstacle == null) _obstacle = gameObject.AddComponent<NavMeshObstacle>();
            _obstacle.shape = NavMeshObstacleShape.Box;
            _obstacle.center = new Vector3(0f, 1.25f, 0f);
            _obstacle.size = new Vector3(data.sizeX, 2.5f, data.sizeZ);
            _obstacle.carving = true;

            SetupHealth(data.maxHP, data.armor, ArmorClass.Structure);
            Health.Damaged += OnDamagedByEnemy;

            _visual = transform.Find("Visual");

            Ring = SelectionRing.Attach(this, Radius + 0.3f);
            Bar = HealthBar.Attach(this, Health, Mathf.Max(2f, data.sizeX * 0.6f), 3.6f);

            if (data.CanAttack)
            {
                _weapon = gameObject.GetComponent<Weapon>();
                if (_weapon == null) _weapon = gameObject.AddComponent<Weapon>();
                var muzzle = transform.Find("Visual/Muzzle");
                _weapon.Init(this, data.damage, data.damageClass, data.attackRange,
                    data.attackCooldown, data.projectileSpeed, muzzle, false);
            }

            if (preBuilt)
            {
                BuildProgress = 1f;
                CompleteConstruction(true);
            }
            else
            {
                Health.SetCurrent(data.maxHP * 0.1f);
                if (_visual != null) _visual.localScale = new Vector3(1f, 0.12f, 1f);
                Bar.SetProgress(0f);
                if (G.Effects != null) _dust = G.Effects.AttachConstructionDust(transform, Data.sizeX);
            }

            FinishInit();
        }

        // ---------- Construction ----------

        /// <summary>Called by a worker standing at the site. Multiple workers don't stack.</summary>
        public void Contribute(float dt)
        {
            if (IsComplete || IsDead || _contributedThisFrame) return;
            _contributedThisFrame = true;
            float delta = dt / Data.buildTime;
            BuildProgress = Mathf.Clamp01(BuildProgress + delta);
            Health.Heal(Data.maxHP * 0.9f * delta);
            if (_visual != null)
                _visual.localScale = new Vector3(1f, Mathf.Lerp(0.12f, 1f, BuildProgress), 1f);
            Bar.SetProgress(BuildProgress);
            if (BuildProgress >= 1f) CompleteConstruction(false);
        }

        void LateUpdate() => _contributedThisFrame = false;

        public bool CancelConstruction(float refundFraction = 0.75f)
        {
            if (IsComplete || IsDead || Data == null) return false;
            int refund = Mathf.RoundToInt(Data.mineralCost * Mathf.Clamp01(refundFraction));
            if (refund > 0) G.Bank(Faction).Refund(refund);
            if (_dust != null) { Destroy(_dust.gameObject); _dust = null; }
            if (Faction == Faction.Player)
            {
                if (G.Hud != null) G.Hud.Notify($"{Data.displayName} canceled (+{refund} minerals)");
                if (G.Audio != null) G.Audio.Play("click", 0.55f);
            }
            RetireSilently();
            return true;
        }

        void CompleteConstruction(bool silent)
        {
            if (IsComplete) return;
            IsComplete = true;
            BuildProgress = 1f;
            if (_visual != null) _visual.localScale = Vector3.one;
            Bar.SetProgress(-1f);
            if (_dust != null) { _dust.Stop(); Destroy(_dust.gameObject, 2f); _dust = null; }

            if (Data.supplyProvided > 0)
            {
                G.Bank(Faction).AddSupplyCap(Data.supplyProvided);
                _supplyGiven = Data.supplyProvided;
            }

            if (!silent)
            {
                if (Faction == Faction.Player && G.Audio != null) G.Audio.Play("build_done");
                if (Faction == Faction.Player && G.Hud != null) G.Hud.Notify($"{Data.displayName} complete");
            }
        }

        // ---------- Training ----------

        public bool CanQueue(UnitData unit)
        {
            if (!IsComplete || IsDead || Queue.Count >= MaxQueue) return false;
            return G.Bank(Faction).CanAfford(unit.mineralCost, unit.supplyCost);
        }

        public bool TryQueue(UnitData unit)
        {
            if (!CanQueue(unit)) return false;
            if (!G.Bank(Faction).TrySpend(unit.mineralCost, unit.supplyCost)) return false;
            Queue.Add(unit);
            return true;
        }

        public void SetRally(Vector3 point)
        {
            RallyPoint = point;
            RefreshRallyFlag();
        }

        void RefreshRallyFlag()
        {
            if (_rallyFlag == null && RallyPoint.HasValue)
            {
                _rallyFlag = new GameObject("RallyFlag");
                var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Destroy(pole.GetComponent<Collider>());
                pole.transform.SetParent(_rallyFlag.transform, false);
                pole.transform.localPosition = new Vector3(0f, 0.7f, 0f);
                pole.transform.localScale = new Vector3(0.08f, 0.7f, 0.08f);
                pole.GetComponent<Renderer>().sharedMaterial = MaterialLibrary.Get("metal_light");
                var flag = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(flag.GetComponent<Collider>());
                flag.transform.SetParent(_rallyFlag.transform, false);
                flag.transform.localPosition = new Vector3(0.25f, 1.2f, 0f);
                flag.transform.localScale = new Vector3(0.5f, 0.3f, 0.05f);
                flag.GetComponent<Renderer>().sharedMaterial = MaterialLibrary.Get("rally");
            }
            if (_rallyFlag != null)
            {
                _rallyFlag.SetActive(RallyPoint.HasValue && G.Selection != null && G.Selection.IsSelected(this));
                if (RallyPoint.HasValue) _rallyFlag.transform.position = RallyPoint.Value;
            }
        }

        void Update()
        {
            if (IsDead) return;

            // Bubble structures grow themselves — no worker required.
            if (!IsComplete)
            {
                if (Data.selfBuild) Contribute(Time.deltaTime);
                return;
            }

            TickFlight();
            if (Flight == FlightState.Grounded)
            {
                TickTraining();
                TickTurret();
            }
            if (_rallyFlag != null) RefreshRallyFlag();
        }

        // ---------- Lift-off / flight ----------

        public void CommandLiftOff()
        {
            if (!CanLift || Flight != FlightState.Grounded) return;
            Flight = FlightState.Lifting;
            _hasFlightDest = false;
            _obstacle.enabled = false; // un-carve: ground under the building opens up
            if (Faction == Faction.Player && G.Audio != null) G.Audio.Play("build_place", 0.6f);
            if (G.Selection != null && G.Selection.IsSelected(this)) G.Selection.RaiseChanged();
        }

        /// <summary>While airborne: fly toward a point (right-click).</summary>
        public void CommandFlyTo(Vector3 dest)
        {
            if (!IsAirborne) return;
            _flightDest = new Vector3(dest.x, 0f, dest.z);
            _hasFlightDest = true;
            if (Flight == FlightState.Landing) Flight = FlightState.Flying; // abort landing
        }

        /// <summary>Descend onto a spot (validity is checked by the caller).</summary>
        public void CommandLandAt(Vector3 spot)
        {
            if (!IsAirborne) return;
            _landSpot = new Vector3(spot.x, 0f, spot.z);
            _flightDest = _landSpot;
            _hasFlightDest = true;
            Flight = FlightState.Flying; // fly there first; Landing starts on arrival
            _landing = true;
        }
        bool _landing;

        void TickFlight()
        {
            if (Flight == FlightState.Grounded) return;

            float groundY = MapBuilder.GroundHeight(transform.position.x, transform.position.z);
            float hoverY = groundY + FlyHeight + Mathf.Sin(Time.time * 1.7f) * 0.15f;
            Vector3 p = transform.position;

            switch (Flight)
            {
                case FlightState.Lifting:
                    p.y = Mathf.MoveTowards(p.y, hoverY, LiftSpeed * Time.deltaTime);
                    if (Mathf.Abs(p.y - hoverY) < 0.2f)
                    {
                        Flight = FlightState.Flying;
                        if (G.Selection != null && G.Selection.IsSelected(this)) G.Selection.RaiseChanged();
                    }
                    break;

                case FlightState.Flying:
                    p.y = Mathf.MoveTowards(p.y, hoverY, LiftSpeed * Time.deltaTime);
                    if (_hasFlightDest)
                    {
                        Vector3 planar = new Vector3(_flightDest.x - p.x, 0f, _flightDest.z - p.z);
                        float step = FlySpeed * Time.deltaTime;
                        if (planar.magnitude <= step)
                        {
                            p.x = _flightDest.x; p.z = _flightDest.z;
                            _hasFlightDest = false;
                            if (_landing)
                            {
                                // re-validate the pad right before descending
                                if (G.Placer != null && G.Placer.IsValidAt(Data, _landSpot))
                                    Flight = FlightState.Landing;
                                else
                                {
                                    _landing = false;
                                    if (Faction == Faction.Player && G.Hud != null) G.Hud.Notify("Landing zone blocked");
                                }
                            }
                        }
                        else p += planar.normalized * step;
                    }
                    break;

                case FlightState.Landing:
                {
                    float targetY = MapBuilder.GroundHeight(_landSpot.x, _landSpot.z) - 0.05f;
                    p.y = Mathf.MoveTowards(p.y, targetY, LiftSpeed * Time.deltaTime);
                    if (Mathf.Abs(p.y - targetY) < 0.02f)
                    {
                        p.y = targetY;
                        Flight = FlightState.Grounded;
                        _landing = false;
                        _obstacle.enabled = true; // carve again
                        if (Faction == Faction.Player && G.Audio != null) G.Audio.Play("build_done", 0.5f);
                        if (G.Selection != null && G.Selection.IsSelected(this)) G.Selection.RaiseChanged();
                    }
                    break;
                }
            }
            transform.position = p;
        }

        void TickTraining()
        {
            if (Queue.Count == 0) { CurrentTrainProgress = 0f; Bar.SetProgress(-1f); return; }
            var unit = Queue[0];
            CurrentTrainProgress += Time.deltaTime / unit.trainTime;
            if (Faction == Faction.Player) Bar.SetProgress(CurrentTrainProgress);
            if (CurrentTrainProgress >= 1f)
            {
                CurrentTrainProgress = 0f;
                Queue.RemoveAt(0);
                Bar.SetProgress(-1f);
                SpawnTrained(unit);
            }
        }

        void SpawnTrained(UnitData unitData)
        {
            Vector3 exit = FindExitPoint();
            var u = UnitFactory.Spawn(unitData, Faction, exit);
            if (u == null) { G.Bank(Faction).Refund(0, unitData.supplyCost); return; }

            if (RallyPoint.HasValue)
            {
                if (u is WorkerUnit w)
                {
                    var node = MineralNode.FindNearest(RallyPoint.Value, 6f);
                    if (node != null) w.CommandHarvest(node);
                    else w.CommandMove(RallyPoint.Value);
                }
                else u.CommandMove(RallyPoint.Value);
            }
            else if (u is WorkerUnit w2 && Data.isDropoff)
            {
                var node = MineralNode.FindNearest(transform.position, 25f);
                if (node != null) w2.CommandHarvest(node);
            }
            if (Faction == Faction.Player && G.Audio != null) G.Audio.Play("train_done", 0.6f);
        }

        Vector3 FindExitPoint()
        {
            Vector3 basePos = transform.position + transform.forward * (Data.sizeZ * 0.5f + 1.2f);
            for (int i = 0; i < 8; i++)
            {
                Vector3 candidate = basePos + Quaternion.Euler(0f, i * 45f, 0f) * Vector3.forward * (i * 0.4f);
                if (NavMesh.SamplePosition(candidate, out var hit, 3f, NavMesh.AllAreas))
                    return hit.position;
            }
            return basePos;
        }

        // ---------- Turret ----------

        void TickTurret()
        {
            if (_weapon == null) return;
            _scanTimer -= Time.deltaTime;
            if (_turretTarget == null || _turretTarget.IsDead || !_weapon.InRange(_turretTarget))
            {
                if (_scanTimer > 0f) return;
                _scanTimer = 0.4f;
                _turretTarget = FindTurretTarget();
            }
            if (_turretTarget != null)
            {
                var head = transform.Find("Visual/Head");
                if (head != null)
                {
                    Vector3 dir = _turretTarget.Position - head.position;
                    dir.y = 0f;
                    if (dir.sqrMagnitude > 0.01f)
                        head.rotation = Quaternion.Slerp(head.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 8f);
                }
                _weapon.TryFire(_turretTarget);
            }
        }

        Entity FindTurretTarget()
        {
            Entity best = null;
            float bestScore = float.MaxValue;
            float r2 = (_weapon.Range + 0.5f) * (_weapon.Range + 0.5f);
            foreach (var e in Entity.All)
            {
                if (e == null || e.IsDead || e.Faction == Faction || e.Faction == Faction.Neutral) continue;
                float d = (e.Position - transform.position).sqrMagnitude;
                if (d > r2) continue;
                float score = d + (e.IsBuilding ? 100000f : 0f);
                if (score < bestScore) { bestScore = score; best = e; }
            }
            return best;
        }

        // ---------- Damage / death ----------

        void OnDamagedByEnemy(Entity attacker)
        {
            if (attacker == null) return;
            if (G.AI != null && Faction == Faction.Enemy) G.AI.NotifyBaseAttacked(attacker);
            if (Faction == Faction.Player && G.Hud != null) G.Hud.NotifyUnderAttack(transform.position);
            // nearby own combat units respond
            foreach (var e in Entity.All)
                if (e is Unit u && u.Faction == Faction && !u.Data.isWorker)
                    u.NotifyAllyAttacked(attacker);
        }

        protected override void OnDeath(Entity killer)
        {
            if (_supplyGiven > 0) G.Bank(Faction).RemoveSupplyCap((int)_supplyGiven);
            // refund supply of queued units
            foreach (var q in Queue) G.Bank(Faction).ReleaseSupply(q.supplyCost);
            Queue.Clear();
            if (_rallyFlag != null) Destroy(_rallyFlag);
            if (_dust != null) Destroy(_dust.gameObject);
            base.OnDeath(killer);
        }

        // ---------- Static queries ----------

        public static Building FindNearestDropoff(Faction f, Vector3 pos)
        {
            Building best = null;
            float bestD = float.MaxValue;
            foreach (var e in Entity.All)
            {
                if (!(e is Building b) || b.Faction != f || !b.IsComplete || b.IsDead || !b.Data.isDropoff || b.IsAirborne) continue;
                float d = (b.Position - pos).sqrMagnitude;
                if (d < bestD) { bestD = d; best = b; }
            }
            return best;
        }

        public static int CountBuildings(Faction f, bool completeOnly = false)
        {
            int n = 0;
            foreach (var e in Entity.All)
                if (e is Building b && b.Faction == f && !b.IsDead && (!completeOnly || b.IsComplete)) n++;
            return n;
        }
    }
}
