using System.Collections.Generic;
using UnityEngine;

namespace VoidClash
{
    /// <summary>Rule-based AI opponent: runs a worker economy, keeps supply ahead,
    /// builds production + defense, and launches escalating attack waves.
    /// Plays by the same rules as the player (same costs, same build mechanics).</summary>
    public class EnemyAI : MonoBehaviour
    {
        ResourceBank Bank => G.EnemyBank;
        Vector3 _basePos;
        float _tick;
        float _matchTime;

        // wave control (defaults = free play; overridden per campaign mission)
        float _nextWaveTime = 210f;
        float _waveInterval = 110f;
        int _waveSize = 6;
        int _waveGrowth = 4;
        int _workerTarget = 13;
        int _turretTarget = 2;
        int _sensorTarget;
        bool _buildFactory = true;
        float _factoryTime = 150f;
        float _secondRaxTime = 240f;
        float _turretTime = 200f;
        float _expandTime = 360f;
        AIPersonality _personality = AIPersonality.Balanced;
        string[] _armyMixLight = { "soldier", "soldier", "ranged" };
        string[] _armyMixHeavy = { "heavy" };
        Unit _boss;
        float _bossAttackTime = float.MaxValue;
        bool _bossSent;
        bool _bossEscortSent;
        bool _bossWarningSent;
        float _warnedWaveTime = -1f;
        const float WaveWarningLead = 25f;
        const float BossWarningLead = 45f;

        readonly List<Unit> _army = new List<Unit>();
        readonly List<Unit> _waveOut = new List<Unit>();
        bool _defending;
        float _defendUntil;
        Vector3 _threatPos;

        int _turretsBuilt;
        int _sensorsBuilt;
        int _raxCount;
        int _ccCount;
        bool _hasFactory;

        public void Init(Vector3 basePos)
        {
            _basePos = basePos;
            var m = Campaign.Current;
            if (m == null) return;
            _nextWaveTime = m.firstWaveTime;
            _waveInterval = m.waveInterval;
            _waveSize = m.firstWaveSize;
            _waveGrowth = m.waveSizeGrowth;
            _workerTarget = m.aiWorkerCap;
            _turretTarget = m.aiTurrets;
            _buildFactory = m.aiBuildsFactory;
            _personality = m.aiPersonality;
            _armyMixLight = m.armyMix;
            _armyMixHeavy = m.armyMix;
            _bossAttackTime = m.bossAttackTime;
            _bossWarningSent = false;
            _warnedWaveTime = -1f;
            ApplyPersonality();
        }

        void ApplyPersonality()
        {
            _sensorTarget = 0;
            _factoryTime = 150f;
            _secondRaxTime = 240f;
            _turretTime = 200f;
            _expandTime = 360f;

            switch (_personality)
            {
                case AIPersonality.Rusher:
                    _nextWaveTime *= 0.85f;
                    _waveInterval *= 0.85f;
                    _waveSize = Mathf.Max(4, _waveSize - 1);
                    _waveGrowth += 1;
                    _workerTarget = Mathf.Max(9, _workerTarget - 2);
                    _secondRaxTime = 165f;
                    _turretTime = 260f;
                    _expandTime = 430f;
                    break;
                case AIPersonality.Turtle:
                    _waveInterval *= 1.15f;
                    _turretTarget += 2;
                    _sensorTarget = 1;
                    _turretTime = 150f;
                    _expandTime = 420f;
                    break;
                case AIPersonality.Expander:
                    _workerTarget += 2;
                    _sensorTarget = 1;
                    _expandTime = 290f;
                    _turretTime = 185f;
                    break;
                case AIPersonality.Tech:
                    _buildFactory = true;
                    _factoryTime = 115f;
                    _secondRaxTime = 270f;
                    _sensorTarget = 1;
                    _waveInterval *= 1.08f;
                    break;
                case AIPersonality.Swarm:
                    _nextWaveTime *= 0.92f;
                    _waveInterval *= 0.9f;
                    _waveGrowth += 2;
                    _sensorTarget = 1;
                    _turretTarget = Mathf.Max(1, _turretTarget - 1);
                    break;
            }
        }

        /// <summary>Mission 3: the boss unit this AI escorts and eventually unleashes.</summary>
        public void RegisterBoss(Unit boss) => _boss = boss;

        void Update()
        {
            if (G.Game == null || G.Game.IsOver || G.Game.IsPaused) return;
            _matchTime += Time.deltaTime;
            _tick -= Time.deltaTime;
            if (_tick > 0f) return;
            _tick = 1.0f;

            CleanLists();
            RefreshCounts();
            TickEconomy();
            TickProduction();
            TickArmy();
        }

        void CleanLists()
        {
            _army.RemoveAll(u => u == null || u.IsDead);
            _waveOut.RemoveAll(u => u == null || u.IsDead);
        }

        void RefreshCounts()
        {
            _raxCount = 0; _ccCount = 0; _hasFactory = false; _turretsBuilt = 0; _sensorsBuilt = 0;
            foreach (var e in Entity.All)
            {
                if (!(e is Building b) || b.Faction != Faction.Enemy || b.IsDead) continue;
                if (b.Data.id == "cc") _ccCount++;
                else if (b.Data.id == "barracks") _raxCount++;
                else if (b.Data.id == "factory") _hasFactory = true;
                else if (b.Data.id == "turret") _turretsBuilt++;
                else if (b.Data.id == "sensor") _sensorsBuilt++;
            }
        }

        // ---------- Economy ----------

        void TickEconomy()
        {
            var cc = FindBuilding("cc");
            if (cc == null) return;

            int workers = CountWorkers(out var idleWorker);
            if (idleWorker != null)
            {
                var node = MineralNode.FindNearest(idleWorker.Position, 60f);
                if (node != null) idleWorker.CommandHarvest(node);
            }

            // keep training workers (slightly slow early to give the player breathing room)
            int cap = _matchTime < 90f ? Mathf.Min(8, _workerTarget) : _workerTarget;
            if (workers < cap && cc.Queue.Count == 0 && cc.CanQueue(G.DB.Unit("worker")))
                cc.TryQueue(G.DB.Unit("worker"));

            // supply
            var depot = G.DB.Building("depot");
            if (depot != null && Bank.SupplyLeft < 7 && !IsConstructing("depot") && Bank.CanAfford(depot.mineralCost))
                TryBuild("depot");
        }

        int CountWorkers(out WorkerUnit idle)
        {
            idle = null;
            int n = 0;
            foreach (var e in Entity.All)
                if (e is WorkerUnit w && w.Faction == Faction.Enemy && !w.IsDead)
                {
                    n++;
                    if (w.IsIdleForWork && idle == null) idle = w;
                }
            return n;
        }

        // ---------- Buildings / production ----------

        void TickProduction()
        {
            int workers = CountWorkers(out _);

            if (_raxCount == 0 && workers >= 6 && Bank.CanAfford(150) && !IsConstructing("barracks"))
            { TryBuild("barracks"); return; }

            var factory = G.DB.Building("factory");
            if (_raxCount >= 1 && _buildFactory && !_hasFactory && _matchTime > _factoryTime
                && factory != null && Bank.CanAfford(factory.mineralCost) && !IsConstructing("factory"))
            { TryBuild("factory"); return; }

            var barracks = G.DB.Building("barracks");
            if (_raxCount == 1 && _matchTime > _secondRaxTime
                && barracks != null && Bank.CanAfford(barracks.mineralCost * 2) && !IsConstructing("barracks"))
            { TryBuild("barracks"); return; }

            var sensor = G.DB.Building("sensor");
            if (_sensorsBuilt < _sensorTarget && _matchTime > 170f
                && sensor != null && Bank.CanAfford(sensor.mineralCost) && !IsConstructing("sensor"))
            { TryBuild("sensor"); return; }

            var turret = G.DB.Building("turret");
            if (_turretsBuilt < _turretTarget && _matchTime > _turretTime
                && turret != null && Bank.CanAfford(turret.mineralCost) && !IsConstructing("turret"))
            { TryBuild("turret"); return; }

            var cc = G.DB.Building("cc");
            if (_ccCount < 2 && workers >= _workerTarget && _matchTime > _expandTime
                && cc != null && Bank.CanAfford(cc.mineralCost) && !IsConstructing("cc"))
            { TryBuild("cc"); return; }

            // army production from the mission's unit mix
            foreach (var e in Entity.All)
            {
                if (!(e is Building b) || b.Faction != Faction.Enemy || !b.IsComplete || b.IsDead) continue;
                if (b.Data.id == "barracks" && b.Queue.Count == 0)
                {
                    var pick = G.DB.Unit(_armyMixLight[Random.Range(0, _armyMixLight.Length)]);
                    if (pick != null && b.CanQueue(pick)) b.TryQueue(pick);
                    else TryEmergencyDepotFor(pick);
                }
                else if (b.Data.id == "factory" && b.Queue.Count == 0)
                {
                    var pick = Campaign.Current == null
                        ? G.DB.Unit("heavy")
                        : G.DB.Unit(_armyMixHeavy[Random.Range(0, _armyMixHeavy.Length)]);
                    if (pick != null && b.CanQueue(pick)) b.TryQueue(pick);
                    else TryEmergencyDepotFor(pick);
                }
                if (b.Data.CanTrain && !b.RallyPoint.HasValue && b.Data.id != "cc")
                    b.SetRally(_basePos + (Vector3.zero - _basePos).normalized * 10f);
            }

            // adopt newly trained combat units into the army (the boss marches alone)
            foreach (var e in Entity.All)
                if (e is Unit u && !(u is WorkerUnit) && u.Faction == Faction.Enemy && !u.IsDead
                    && u != _boss && !_army.Contains(u) && !_waveOut.Contains(u))
                    _army.Add(u);
        }

        bool IsConstructing(string id)
        {
            foreach (var e in Entity.All)
                if (e is Building b && b.Faction == Faction.Enemy && !b.IsComplete && !b.IsDead && b.Data.id == id)
                    return true;
            return false;
        }

        Building FindBuilding(string id, bool completeOnly = true)
        {
            foreach (var e in Entity.All)
                if (e is Building b && b.Faction == Faction.Enemy && !b.IsDead && b.Data.id == id
                    && (!completeOnly || b.IsComplete))
                    return b;
            return null;
        }

        void TryBuild(string id)
        {
            var data = G.DB.Building(id);
            if (data == null || !Bank.CanAfford(data.mineralCost)) return;

            Vector3? spot = FindBuildSpot(data);
            if (!spot.HasValue) return;

            WorkerUnit builder = PickBuilder(spot.Value);
            if (builder == null) return;

            if (!Bank.TrySpend(data.mineralCost)) return;
            var site = G.Placer.PlaceAt(data, Faction.Enemy, spot.Value);
            builder.CommandBuild(site);
        }

        Vector3? FindBuildSpot(BuildingData data)
        {
            // turrets guard the mineral line / approach; everything else rings the base
            Vector3 anchor = _basePos;
            if (data.id == "turret")
                anchor = _basePos + (Vector3.zero - _basePos).normalized * 9f;
            else if (data.id == "sensor")
                anchor = _basePos + (Vector3.zero - _basePos).normalized * 15f;
            else if (data.id == "cc" && _ccCount > 0)
                anchor = _basePos.x > 0f ? MapBuilder.EnemyExpansionPos : MapBuilder.PlayerExpansionPos;

            int maxRing = data.id == "cc" && _ccCount > 0 ? 3 : (data.id == "sensor" ? 2 : 4);
            float startDist = data.id == "cc" && _ccCount > 0 ? 0f : (data.id == "sensor" ? 2f : 6f);
            for (int ring = 1; ring <= maxRing; ring++)
            {
                float dist = startDist + ring * 4.5f;
                for (int i = 0; i < 12; i++)
                {
                    float ang = i * 30f + ring * 15f;
                    Vector3 p = anchor + Quaternion.Euler(0f, ang, 0f) * Vector3.forward * dist;
                    p = new Vector3(Mathf.Round(p.x), 0f, Mathf.Round(p.z));
                    if (G.Placer.IsValidAt(data, p)) return p;
                }
            }
            return null;
        }

        WorkerUnit PickBuilder(Vector3 near)
        {
            WorkerUnit best = null;
            float bestD = float.MaxValue;
            WorkerUnit fallback = null;
            float fallbackD = float.MaxValue;
            foreach (var e in Entity.All)
                if (e is WorkerUnit w && w.Faction == Faction.Enemy && !w.IsDead)
                {
                    float d = (w.Position - near).sqrMagnitude;
                    if (d < fallbackD) { fallbackD = d; fallback = w; }
                    if (w.State == UnitState.Build) continue;
                    if (d < bestD) { bestD = d; best = w; }
                }
            return best != null ? best : fallback;
        }

        void TryEmergencyDepotFor(UnitData unit)
        {
            if (unit == null || Bank.SupplyLeft >= unit.supplyCost || IsConstructing("depot")) return;
            var depot = G.DB.Building("depot");
            if (depot != null && Bank.CanAfford(depot.mineralCost)) TryBuild("depot");
        }

        // ---------- Army control ----------

        int ArmySupply(List<Unit> list)
        {
            int s = 0;
            foreach (var u in list) if (u != null && !u.IsDead) s += u.Data.supplyCost;
            return s;
        }

        void TickArmy()
        {
            if (_defending)
            {
                if (Time.time > _defendUntil) _defending = false;
                else
                {
                    foreach (var u in _army)
                        if (u.State == UnitState.Idle) u.CommandAttackMove(_threatPos);
                    return;
                }
            }

            int readyArmySupply = ArmySupply(_army);
            if (_nextWaveTime - _matchTime <= WaveWarningLead
                && readyArmySupply >= Mathf.Max(1, _waveSize / 2)
                && Mathf.Abs(_warnedWaveTime - _nextWaveTime) > 0.1f)
            {
                _warnedWaveTime = _nextWaveTime;
                NotifyAttackWarning("Enemy attack force gathering near their base.");
            }

            // launch a wave?
            if (_matchTime >= _nextWaveTime && readyArmySupply >= _waveSize)
            {
                var wave = new List<Unit>(_army);
                _army.Clear();
                _waveOut.AddRange(wave);
                Vector3 target = FindPlayerTarget();
                foreach (var u in wave) u.CommandAttackMove(target);
                NotifyAttackWarning("Enemy attack wave incoming!");
                _nextWaveTime = _matchTime + _waveInterval * Random.Range(0.9f, 1.15f);
                _waveSize = Mathf.Min(_waveSize + _waveGrowth, 34);
            }

            // boss: guards home until its hour comes, then marches on the player
            if (_boss != null && !_boss.IsDead && !_bossSent && !_bossWarningSent
                && _bossAttackTime - _matchTime <= BossWarningLead)
            {
                _bossWarningSent = true;
                NotifyAttackWarning("Warning: the Overlord is stirring.");
            }
            if (_boss != null && !_boss.IsDead && !_bossSent && _matchTime >= _bossAttackTime)
            {
                _bossSent = true;
                _boss.CommandAttackMove(FindPlayerTarget());
                SendBossEscort();
                NotifyAttackWarning("The ground trembles... the OVERLORD is coming!");
            }
            if (_boss != null && !_boss.IsDead && _bossSent && _boss.State == UnitState.Idle)
                _boss.CommandAttackMove(FindPlayerTarget());
            if (_boss != null && !_boss.IsDead && _bossSent)
                KeepBossEscortMoving();

            // idle wave units push on toward the player base
            foreach (var u in _waveOut)
                if (u.State == UnitState.Idle)
                    u.CommandAttackMove(FindPlayerTarget());

            // home guard: idle army units gather at the base entrance
            Vector3 guard = _basePos + (Vector3.zero - _basePos).normalized * 12f;
            foreach (var u in _army)
                if (u.State == UnitState.Idle && Vector3.Distance(u.Position, guard) > 8f)
                    u.CommandAttackMove(guard);
        }

        void SendBossEscort()
        {
            if (_bossEscortSent) return;
            _bossEscortSent = true;
            var escort = new List<Unit>();
            int escortSupply = 0;
            int targetSupply = Mathf.Max(8, _waveSize / 2);
            for (int i = _army.Count - 1; i >= 0 && escortSupply < targetSupply; i--)
            {
                var u = _army[i];
                if (u == null || u.IsDead) continue;
                escort.Add(u);
                escortSupply += u.Data.supplyCost;
                _army.RemoveAt(i);
            }
            _waveOut.AddRange(escort);
            Vector3 target = FindPlayerTarget();
            foreach (var u in escort) u.CommandAttackMove(target);
        }

        void KeepBossEscortMoving()
        {
            Vector3 target = FindPlayerTarget();
            foreach (var u in _waveOut)
            {
                if (u == null || u.IsDead || u == _boss) continue;
                if (Vector3.Distance(u.Position, _boss.Position) < 16f && u.State == UnitState.Idle)
                    u.CommandAttackMove(target);
            }
        }

        void NotifyAttackWarning(string message)
        {
            if (G.Hud != null) G.Hud.Notify(message);
            if (G.Audio != null) G.Audio.Play("error", 0.5f);
        }

        Vector3 FindPlayerTarget()
        {
            // nearest player building to our base; falls back to any player entity
            Entity best = null;
            float bestD = float.MaxValue;
            foreach (var e in Entity.All)
            {
                if (e == null || e.IsDead || e.Faction != Faction.Player) continue;
                float d = (e.Position - _basePos).sqrMagnitude - (e.IsBuilding ? 100f : 0f);
                if (d < bestD) { bestD = d; best = e; }
            }
            return best != null ? best.Position : MapBuilder.PlayerBasePos;
        }

        /// <summary>Called when an enemy building takes damage — rally defenders.</summary>
        public void NotifyBaseAttacked(Entity attacker)
        {
            if (attacker == null || attacker.IsDead) return;
            _defending = true;
            _defendUntil = Time.time + 25f;
            _threatPos = attacker.Position;
            foreach (var u in _army)
                u.CommandAttackMove(_threatPos);
            // recall the wave if our base is under serious threat and the wave is still close
            foreach (var u in _waveOut)
                if (Vector3.Distance(u.Position, _basePos) < 35f)
                    u.CommandAttackMove(_threatPos);
        }
    }
}
