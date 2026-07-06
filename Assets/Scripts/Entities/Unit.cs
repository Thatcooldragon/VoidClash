using UnityEngine;
using UnityEngine.AI;

namespace VoidClash
{
    public enum UnitState { Idle, Move, AttackMove, Attack, Hold, Harvest, Build }

    /// <summary>A mobile entity: NavMeshAgent movement + combat state machine.
    /// Workers use the WorkerUnit subclass for harvesting/building.</summary>
    public class Unit : Entity
    {
        public UnitData Data { get; private set; }
        public NavMeshAgent Agent { get; private set; }
        public Weapon Weapon { get; private set; }
        public UnitState State { get; protected set; } = UnitState.Idle;

        public override bool IsBuilding => false;
        public override string DisplayName => Data != null ? Data.displayName : name;
        public override ArmorClass ArmorClass => Data != null ? Data.armorClass : ArmorClass.Light;

        protected Entity AttackTarget;
        protected Vector3 MoveDest;
        protected Vector3 AttackMoveDest;
        protected Vector3 IdleAnchor;

        float _scanTimer;
        const float ScanInterval = 0.3f;
        const float AggroBonus = 4f;

        public virtual void Init(UnitData data, Faction faction)
        {
            Data = data;
            Faction = faction;
            VisionRadius = data.visionRadius;
            Radius = 0.55f * data.bodyScale;
            gameObject.layer = LayerMask.NameToLayer("Units");

            var col = gameObject.GetComponent<CapsuleCollider>();
            if (col == null) col = gameObject.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.8f * data.bodyScale, 0f);
            col.height = 1.6f * data.bodyScale;
            col.radius = Radius;

            Agent = gameObject.GetComponent<NavMeshAgent>();
            if (Agent == null) Agent = gameObject.AddComponent<NavMeshAgent>();
            Agent.speed = data.moveSpeed;
            Agent.acceleration = 24f;
            Agent.angularSpeed = 720f;
            Agent.radius = Mathf.Max(0.3f, Radius * 0.8f);
            Agent.height = 1.6f * data.bodyScale;
            Agent.obstacleAvoidanceType = ObstacleAvoidanceType.MedQualityObstacleAvoidance;
            Agent.avoidancePriority = Random.Range(30, 70);
            Agent.stoppingDistance = 0.3f;

            // Flyers hover above the field and don't jostle with ground units.
            if (data.flying)
            {
                Agent.baseOffset = data.hoverHeight;
                Agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
                Agent.avoidancePriority = 5;
                if (col != null) col.center = new Vector3(0f, 0.8f * data.bodyScale + data.hoverHeight, 0f);
            }

            SetupHealth(data.maxHP, data.armor, data.armorClass);

            Weapon = gameObject.GetComponent<Weapon>();
            if (Weapon == null) Weapon = gameObject.AddComponent<Weapon>();
            var muzzle = transform.Find("Visual/Muzzle");
            Weapon.Init(this, data.damage, data.damageClass, data.attackRange, data.attackCooldown,
                data.projectileSpeed, muzzle, data.IsMelee);

            Ring = SelectionRing.Attach(this, Radius + 0.25f);
            Bar = HealthBar.Attach(this, Health, 1.2f * data.bodyScale, 2.1f * data.bodyScale);

            IdleAnchor = transform.position;
            _scanTimer = Random.value * ScanInterval;
            FinishInit();
        }

        protected override void OnDeath(Entity killer)
        {
            // free this unit's supply (reserved at queue time or at initial spawn)
            if (!IsDead && Data != null && G.PlayerBank != null && G.EnemyBank != null && Faction != Faction.Neutral)
                G.Bank(Faction).ReleaseSupply(Data.supplyCost);
            base.OnDeath(killer);
        }

        // ---------- Commands ----------

        public virtual void CommandMove(Vector3 dest)
        {
            State = UnitState.Move;
            AttackTarget = null;
            MoveDest = dest;
            SetDestination(dest, 0.3f);
        }

        public virtual void CommandAttackMove(Vector3 dest)
        {
            if (!Data.canAttack) { CommandMove(dest); return; }
            State = UnitState.AttackMove;
            AttackTarget = null;
            AttackMoveDest = dest;
            SetDestination(dest, 0.3f);
        }

        public virtual void CommandAttack(Entity target)
        {
            if (!Data.canAttack || target == null) return;
            State = UnitState.Attack;
            AttackTarget = target;
            AttackMoveDest = target.Position;
        }

        public virtual void CommandStop()
        {
            State = UnitState.Idle;
            AttackTarget = null;
            IdleAnchor = transform.position;
            StopAgent();
        }

        public virtual void CommandHold()
        {
            State = UnitState.Hold;
            AttackTarget = null;
            StopAgent();
        }

        // ---------- Update loop ----------

        protected virtual void Update()
        {
            if (IsDead || Data == null) return;
            switch (State)
            {
                case UnitState.Idle: TickIdle(); break;
                case UnitState.Move: TickMove(); break;
                case UnitState.AttackMove: TickAttackMove(); break;
                case UnitState.Attack: TickAttack(); break;
                case UnitState.Hold: TickHold(); break;
            }
        }

        void TickIdle()
        {
            if (!Data.canAttack || Data.isWorker) return; // workers never auto-aggro
            if (ScanTick())
            {
                var enemy = FindTargetInRange(Data.attackRange + AggroBonus);
                if (enemy != null)
                {
                    AttackTarget = enemy;
                    State = UnitState.Attack;
                }
            }
        }

        void TickMove()
        {
            if (Arrived())
            {
                State = UnitState.Idle;
                IdleAnchor = transform.position;
                StopAgent();
            }
        }

        void TickAttackMove()
        {
            if (ScanTick())
            {
                var enemy = FindTargetInRange(Mathf.Max(Data.attackRange + AggroBonus, 9f));
                if (enemy != null)
                {
                    AttackTarget = enemy;
                    State = UnitState.Attack;
                    return;
                }
            }
            if (Arrived())
            {
                State = UnitState.Idle;
                IdleAnchor = transform.position;
                StopAgent();
            }
        }

        void TickAttack()
        {
            if (AttackTarget == null || AttackTarget.IsDead)
            {
                AttackTarget = null;
                // resume attack-move if we were sweeping, else idle in place
                if (Vector3.Distance(transform.position, AttackMoveDest) > 2f)
                {
                    State = UnitState.AttackMove;
                    SetDestination(AttackMoveDest, 0.3f);
                }
                else CommandStop();
                return;
            }

            if (Weapon.InRange(AttackTarget))
            {
                StopAgent();
                FaceTowards(AttackTarget.Position);
                Weapon.TryFire(AttackTarget);
            }
            else
            {
                SetDestination(AttackTarget.Position, Mathf.Max(0.3f, Weapon.Range * 0.8f));
            }
        }

        void TickHold()
        {
            StopAgent();
            if (!Data.canAttack) return;
            if (AttackTarget != null && !AttackTarget.IsDead && Weapon.InRange(AttackTarget))
            {
                FaceTowards(AttackTarget.Position);
                Weapon.TryFire(AttackTarget);
                return;
            }
            if (ScanTick())
                AttackTarget = FindTargetInRange(Data.attackRange);
        }

        // ---------- Helpers ----------

        protected bool ScanTick()
        {
            _scanTimer -= Time.deltaTime;
            if (_scanTimer > 0f) return false;
            _scanTimer = ScanInterval;
            return true;
        }

        protected Entity FindTargetInRange(float range)
        {
            Entity best = null;
            float bestScore = float.MaxValue;
            float r2 = range * range;
            var list = Entity.All;
            for (int i = 0; i < list.Count; i++)
            {
                var e = list[i];
                if (e == null || e.IsDead || e.Faction == Faction || e.Faction == Faction.Neutral) continue;
                float d = (e.Position - transform.position).sqrMagnitude;
                if (d > r2) continue;
                // prefer units over buildings, closer over farther
                float score = d + (e.IsBuilding ? 4000f : 0f);
                if (score < bestScore) { bestScore = score; best = e; }
            }
            return best;
        }

        protected void SetDestination(Vector3 dest, float stopDist)
        {
            if (Agent == null || !Agent.isOnNavMesh) return;
            Agent.stoppingDistance = stopDist;
            Agent.isStopped = false;
            Agent.SetDestination(dest);
        }

        protected void StopAgent()
        {
            if (Agent == null || !Agent.isOnNavMesh) return;
            if (Agent.hasPath) Agent.ResetPath();
            Agent.isStopped = true;
        }

        protected bool Arrived()
        {
            if (Agent == null || !Agent.isOnNavMesh) return true;
            if (Agent.pathPending) return false;
            return Agent.remainingDistance <= Agent.stoppingDistance + 0.15f;
        }

        protected void FaceTowards(Vector3 pos)
        {
            Vector3 dir = pos - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) return;
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(dir), Time.deltaTime * 10f);
        }

        /// <summary>Called when this unit's faction is attacked nearby — join the fight if idle.</summary>
        public void NotifyAllyAttacked(Entity attacker)
        {
            if (!Data.canAttack || attacker == null || attacker.IsDead) return;
            if (State != UnitState.Idle && State != UnitState.AttackMove) return;
            if (Vector3.Distance(transform.position, attacker.Position) > 14f) return;
            AttackTarget = attacker;
            State = UnitState.Attack;
        }
    }
}
