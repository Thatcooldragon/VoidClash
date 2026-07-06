using UnityEngine;
using UnityEngine.AI;

namespace VoidClash
{
    /// <summary>Worker: harvest cycle (node → mine 2s → carry 5 → dropoff) and construction.</summary>
    public class WorkerUnit : Unit
    {
        enum WorkStep { None, ToNode, Mining, ToDropoff, ToSite, Building }

        WorkStep _step = WorkStep.None;
        MineralNode _node;
        Building _site;
        Building _dropoff;
        float _mineTimer;
        int _carrying;
        GameObject _carryVisual;
        Vector3 _workTarget;
        Vector3 _lastProgressPos;
        float _stallTimer;
        int _failedArrivals;
        MineralNode _resumeNode;

        const int CarryMax = 5;
        const float MineTime = 2f;
        const float InteractRange = 1.6f;
        const float InteractDist = 3.0f; // max gap between worker and building footprint edge
        const float StallSeconds = 3.5f;
        const float ProgressEpsilon = 0.08f;

        public bool IsIdleForWork => State == UnitState.Idle;
        public MineralNode CurrentNode => _node;
        public string WorkStatus
        {
            get
            {
                if (State == UnitState.Harvest)
                {
                    if (_step == WorkStep.ToNode) return "Harvest: moving to crystals";
                    if (_step == WorkStep.Mining) return "Harvest: mining";
                    if (_step == WorkStep.ToDropoff) return _carrying > 0 ? "Harvest: returning cargo" : "Harvest: finding dropoff";
                }
                if (State == UnitState.Build)
                {
                    if (_step == WorkStep.ToSite) return "Build: moving to site";
                    if (_step == WorkStep.Building) return _site != null ? $"Build: {_site.DisplayName} {(int)(_site.BuildProgress * 100f)}%" : "Build: working";
                }
                if (State == UnitState.Idle) return "Idle worker";
                return State.ToString();
            }
        }

        public override void Init(UnitData data, Faction faction)
        {
            base.Init(data, faction);
            _carryVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _carryVisual.name = "CarryCrystal";
            Destroy(_carryVisual.GetComponent<Collider>());
            _carryVisual.transform.SetParent(transform, false);
            _carryVisual.transform.localPosition = new Vector3(0f, 1.15f * data.bodyScale, -0.35f);
            _carryVisual.transform.localRotation = Quaternion.Euler(45f, 45f, 0f);
            _carryVisual.transform.localScale = Vector3.one * 0.3f;
            _carryVisual.GetComponent<Renderer>().sharedMaterial = MaterialLibrary.Get("crystal");
            _carryVisual.SetActive(false);
        }

        public void CommandHarvest(MineralNode node)
        {
            if (node == null || node.Depleted) node = MineralNode.FindNearest(transform.position);
            if (node == null) { CommandStop(); return; }
            _node = node;
            _site = null;
            State = UnitState.Harvest;
            _step = _carrying >= CarryMax ? WorkStep.ToDropoff : WorkStep.ToNode;
            if (_step == WorkStep.ToNode) SetWorkDestination(node.transform.position, InteractRange);
            else GoToDropoff();
        }

        public void CommandBuild(Building site)
        {
            if (site == null || site.IsComplete) return;
            _resumeNode = _node != null && !_node.Depleted ? _node : MineralNode.FindNearest(transform.position, 35f);
            _site = site;
            _node = null;
            State = UnitState.Build;
            _step = WorkStep.ToSite;
            SetWorkDestination(FindBuildingApproach(site), 0.5f);
        }

        public override void CommandMove(Vector3 dest) { ClearWork(); base.CommandMove(dest); }
        public override void CommandAttackMove(Vector3 dest) { ClearWork(); base.CommandAttackMove(dest); }
        public override void CommandAttack(Entity target) { ClearWork(); base.CommandAttack(target); }
        public override void CommandStop() { ClearWork(); base.CommandStop(); }
        public override void CommandHold() { ClearWork(); base.CommandHold(); }

        void ClearWork()
        {
            _step = WorkStep.None;
            _site = null;
            _resumeNode = null;
            ResetWorkWatch();
        }

        protected override void Update()
        {
            if (IsDead || Data == null) return;
            if (State == UnitState.Harvest) TickHarvest();
            else if (State == UnitState.Build) TickBuild();
            else base.Update();
        }

        void TickHarvest()
        {
            switch (_step)
            {
                case WorkStep.ToNode:
                    if (_node == null || _node.Depleted) { RetargetNode(); return; }
                    if (WorkPathStalled()) { RetargetNode(); return; }
                    if (CloseTo(_node.transform.position, InteractRange + _node.Radius))
                    {
                        StopAgent();
                        ResetWorkWatch();
                        FaceTowards(_node.transform.position);
                        _step = WorkStep.Mining;
                        _mineTimer = MineTime;
                    }
                    else if (Arrived())
                    {
                        if (++_failedArrivals >= 3) RetargetNode();
                        else SetWorkDestination(_node.transform.position, InteractRange, false);
                    }
                    break;

                case WorkStep.Mining:
                    if (_node == null || _node.Depleted) { RetargetNode(); return; }
                    if (!CloseTo(_node.transform.position, InteractRange + _node.Radius + 1.4f))
                    {
                        _step = WorkStep.ToNode;
                        SetWorkDestination(_node.transform.position, InteractRange);
                        return;
                    }
                    _mineTimer -= Time.deltaTime;
                    if (G.Effects != null && Random.value < Time.deltaTime * 3f)
                        G.Effects.SpawnHarvestSparkle(_node.transform.position + Vector3.up * 0.7f);
                    if (_mineTimer <= 0f)
                    {
                        _carrying = _node.Harvest(CarryMax);
                        if (_carrying > 0)
                        {
                            _carryVisual.SetActive(true);
                            GoToDropoff();
                        }
                        else RetargetNode();
                    }
                    break;

                case WorkStep.ToDropoff:
                    if (_dropoff == null || _dropoff.IsDead || _dropoff.IsAirborne) { GoToDropoff(); if (_dropoff == null) return; }
                    if (WorkPathStalled()) { GoToDropoff(); return; }
                    if (_dropoff.DistanceToEdge(transform.position) <= InteractDist)
                    {
                        G.Bank(Faction).AddMinerals(_carrying);
                        if (Faction == Faction.Player && G.Audio != null) G.Audio.Play("deposit", 0.35f);
                        _carrying = 0;
                        _carryVisual.SetActive(false);
                        if (_node == null || _node.Depleted) RetargetNode();
                        else
                        {
                            _step = WorkStep.ToNode;
                            SetWorkDestination(_node.transform.position, InteractRange);
                        }
                    }
                    else if (Arrived() && _dropoff != null)
                    {
                        if (++_failedArrivals >= 3) GoToDropoff();
                        else SetWorkDestination(FindBuildingApproach(_dropoff), 0.5f, false);
                    }
                    break;
            }
        }

        void TickBuild()
        {
            if (_site == null || _site.IsDead || _site.IsComplete)
            {
                var finished = _site;
                var resume = _resumeNode;
                ClearWork();
                // go back to mining if there are minerals nearby
                var node = resume != null && !resume.Depleted ? resume : MineralNode.FindNearest(transform.position, 30f);
                if (node != null && finished != null && finished.IsComplete) CommandHarvest(node);
                else CommandStop();
                return;
            }
            if (_step == WorkStep.ToSite)
            {
                if (WorkPathStalled())
                {
                    SetWorkDestination(FindBuildingApproach(_site), 0.5f);
                    return;
                }
                if (_site.DistanceToEdge(transform.position) <= InteractDist)
                {
                    StopAgent();
                    ResetWorkWatch();
                    FaceTowards(_site.transform.position);
                    _step = WorkStep.Building;
                }
                else if (Arrived())
                {
                    if (++_failedArrivals >= 3)
                    {
                        CommandStop();
                        if (Faction == Faction.Player && G.Hud != null) G.Hud.Notify("Builder cannot reach site");
                    }
                    else SetWorkDestination(FindBuildingApproach(_site), 0.5f, false);
                }
            }
            else if (_step == WorkStep.Building)
            {
                if (_site.DistanceToEdge(transform.position) > InteractDist + 1.5f)
                {
                    _step = WorkStep.ToSite;
                    SetWorkDestination(FindBuildingApproach(_site), 0.5f);
                    return;
                }
                FaceTowards(_site.transform.position);
                _site.Contribute(Time.deltaTime);
            }
        }

        void GoToDropoff()
        {
            _dropoff = Building.FindNearestDropoff(Faction, transform.position);
            if (_dropoff == null)
            {
                // no dropoff left — just idle
                State = UnitState.Idle;
                _step = WorkStep.None;
                StopAgent();
                return;
            }
            _step = WorkStep.ToDropoff;
            SetWorkDestination(FindBuildingApproach(_dropoff), 0.5f);
        }

        void RetargetNode()
        {
            var node = MineralNode.FindNearest(transform.position, 40f);
            if (node == null)
            {
                if (_carrying > 0) { GoToDropoff(); return; }
                State = UnitState.Idle;
                _step = WorkStep.None;
                StopAgent();
                return;
            }
            _node = node;
            _step = _carrying >= CarryMax ? WorkStep.ToDropoff : WorkStep.ToNode;
            if (_step == WorkStep.ToNode) SetWorkDestination(node.transform.position, InteractRange);
            else GoToDropoff();
        }

        void SetWorkDestination(Vector3 dest, float stopDist, bool resetFailures = true)
        {
            _workTarget = dest;
            ResetWorkWatch(resetFailures);
            SetDestination(dest, stopDist);
        }

        void ResetWorkWatch(bool resetFailures = true)
        {
            _lastProgressPos = transform.position;
            _stallTimer = 0f;
            if (resetFailures) _failedArrivals = 0;
        }

        bool WorkPathStalled()
        {
            if (Agent == null || !Agent.isOnNavMesh) return false;
            if (!Agent.pathPending)
            {
                if (Agent.pathStatus == NavMeshPathStatus.PathInvalid) return true;
                if (Agent.pathStatus == NavMeshPathStatus.PathPartial && Arrived()) return true;
            }
            if (Agent.isStopped || !Agent.hasPath) { ResetWorkWatch(false); return false; }
            if (Agent.remainingDistance <= Agent.stoppingDistance + 0.6f) { ResetWorkWatch(false); return false; }

            Vector3 moved = transform.position - _lastProgressPos;
            moved.y = 0f;
            if (moved.magnitude > ProgressEpsilon)
            {
                _lastProgressPos = transform.position;
                _stallTimer = 0f;
                return false;
            }

            _stallTimer += Time.deltaTime;
            return _stallTimer >= StallSeconds && Vector3.Distance(transform.position, _workTarget) > Agent.stoppingDistance + 0.75f;
        }

        Vector3 FindBuildingApproach(Building b)
        {
            if (b == null) return transform.position;
            Vector3 center = b.transform.position;
            float radius = Mathf.Max(b.Data.sizeX, b.Data.sizeZ) * 0.5f + 1.8f;
            Vector3 best = center;
            float bestD = float.MaxValue;
            var path = new NavMeshPath();

            for (int i = 0; i < 16; i++)
            {
                float ang = i * 22.5f;
                Vector3 probe = center + Quaternion.Euler(0f, ang, 0f) * Vector3.forward * radius;
                if (!NavMesh.SamplePosition(probe, out var hit, 1.8f, NavMesh.AllAreas)) continue;
                if (Agent != null && Agent.isOnNavMesh && NavMesh.CalculatePath(transform.position, hit.position, NavMesh.AllAreas, path)
                    && path.status == NavMeshPathStatus.PathInvalid)
                    continue;
                float d = (hit.position - transform.position).sqrMagnitude;
                if (d < bestD) { bestD = d; best = hit.position; }
            }

            return bestD < float.MaxValue ? best : center;
        }

        bool CloseTo(Vector3 pos, float range)
        {
            Vector3 d = pos - transform.position;
            d.y = 0f;
            return d.magnitude <= range + 0.6f;
        }
    }
}
