using UnityEngine;

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

        const int CarryMax = 5;
        const float MineTime = 2f;
        const float InteractRange = 1.6f;
        const float InteractDist = 3.0f; // max gap between worker and building footprint edge

        public bool IsIdleForWork => State == UnitState.Idle;
        public MineralNode CurrentNode => _node;

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
            if (_step == WorkStep.ToNode) SetDestination(node.transform.position, InteractRange);
            else GoToDropoff();
        }

        public void CommandBuild(Building site)
        {
            if (site == null || site.IsComplete) return;
            _site = site;
            _node = null;
            State = UnitState.Build;
            _step = WorkStep.ToSite;
            SetDestination(site.transform.position, site.ApproachRange);
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
                    if (CloseTo(_node.transform.position, InteractRange + _node.Radius))
                    {
                        StopAgent();
                        FaceTowards(_node.transform.position);
                        _step = WorkStep.Mining;
                        _mineTimer = MineTime;
                    }
                    else if (Arrived()) SetDestination(_node.transform.position, InteractRange);
                    break;

                case WorkStep.Mining:
                    if (_node == null || _node.Depleted) { RetargetNode(); return; }
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
                            SetDestination(_node.transform.position, InteractRange);
                        }
                    }
                    else if (Arrived() && _dropoff != null)
                        SetDestination(_dropoff.transform.position, _dropoff.ApproachRange);
                    break;
            }
        }

        void TickBuild()
        {
            if (_site == null || _site.IsDead || _site.IsComplete)
            {
                var finished = _site;
                ClearWork();
                // go back to mining if there are minerals nearby
                var node = MineralNode.FindNearest(transform.position, 30f);
                if (node != null && finished != null && finished.IsComplete) CommandHarvest(node);
                else CommandStop();
                return;
            }
            if (_step == WorkStep.ToSite)
            {
                if (_site.DistanceToEdge(transform.position) <= InteractDist)
                {
                    StopAgent();
                    FaceTowards(_site.transform.position);
                    _step = WorkStep.Building;
                }
                else if (Arrived()) SetDestination(_site.transform.position, _site.ApproachRange);
            }
            else if (_step == WorkStep.Building)
            {
                if (_site.DistanceToEdge(transform.position) > InteractDist + 1.5f)
                {
                    _step = WorkStep.ToSite;
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
            SetDestination(_dropoff.transform.position, _dropoff.ApproachRange);
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
            if (_step == WorkStep.ToNode) SetDestination(node.transform.position, InteractRange);
            else GoToDropoff();
        }

        bool CloseTo(Vector3 pos, float range)
        {
            Vector3 d = pos - transform.position;
            d.y = 0f;
            return d.magnitude <= range + 0.6f;
        }
    }
}
