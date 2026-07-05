using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoidClash
{
    /// <summary>Base for every ownable thing on the map (units and buildings).
    /// Maintains the global registry used by selection, AI, fog and win conditions.</summary>
    public abstract class Entity : MonoBehaviour
    {
        public static readonly List<Entity> All = new List<Entity>();
        public static event Action<Entity> AnySpawned;
        public static event Action<Entity> AnyDied;

        public Faction Faction { get; protected set; }
        public Health Health { get; protected set; }
        public float VisionRadius { get; protected set; } = 10f;
        public abstract bool IsBuilding { get; }
        public abstract string DisplayName { get; }
        public abstract ArmorClass ArmorClass { get; }
        /// <summary>Approximate selection/targeting radius in world units.</summary>
        public float Radius { get; protected set; } = 0.6f;

        public bool IsDead { get; private set; }

        Renderer[] _renderers;
        bool _visibleToPlayer = true;
        public bool VisibleToPlayer => _visibleToPlayer;

        protected SelectionRing Ring;
        public HealthBar Bar { get; protected set; }

        public static void ClearRegistry()
        {
            All.Clear();
            AnySpawned = null;
            AnyDied = null;
        }

        protected virtual void OnEnable()
        {
            if (!All.Contains(this)) All.Add(this);
        }

        protected virtual void OnDisable()
        {
            All.Remove(this);
        }

        protected void FinishInit()
        {
            _renderers = GetComponentsInChildren<Renderer>(true);
            AnySpawned?.Invoke(this);
            if (Faction == Faction.Enemy) SetVisibleToPlayer(false);
        }

        protected void SetupHealth(int maxHP, int armor, ArmorClass ac)
        {
            Health = gameObject.GetComponent<Health>();
            if (Health == null) Health = gameObject.AddComponent<Health>();
            Health.Init(this, maxHP, armor, ac);
            Health.Died += OnDeath;
        }

        protected virtual void OnDeath(Entity killer)
        {
            if (IsDead) return;
            IsDead = true;
            AnyDied?.Invoke(this);
            if (G.Effects != null)
                G.Effects.SpawnExplosion(transform.position + Vector3.up * 0.6f, IsBuilding ? 2.2f : 1f, Faction);
            if (G.Audio != null && (_visibleToPlayer || Faction == Faction.Player))
                G.Audio.PlayAt("explosion", transform.position);
            if (G.Selection != null) G.Selection.NotifyDied(this);
            Destroy(gameObject);
        }

        /// <summary>Fog of war visibility for enemy entities.</summary>
        public void SetVisibleToPlayer(bool visible)
        {
            if (_visibleToPlayer == visible) return;
            _visibleToPlayer = visible;
            if (_renderers != null)
                foreach (var r in _renderers)
                    if (r != null) r.enabled = visible;
            if (Bar != null) Bar.SetSuppressed(!visible);
        }

        public void SetSelected(bool selected)
        {
            if (Ring != null) Ring.SetSelected(selected);
            if (Bar != null) Bar.SetSelected(selected);
        }

        public void SetHovered(bool hovered)
        {
            if (Ring != null) Ring.SetHovered(hovered);
        }

        public Vector3 Position => transform.position;
    }
}
