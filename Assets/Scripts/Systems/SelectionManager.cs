using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoidClash
{
    /// <summary>Holds the current selection + control groups. Mouse handling lives in InputController.</summary>
    public class SelectionManager : MonoBehaviour
    {
        public readonly List<Entity> Selected = new List<Entity>();
        readonly Dictionary<int, List<Entity>> _groups = new Dictionary<int, List<Entity>>();
        Entity _hovered;

        public event Action Changed;

        public bool HasUnitsSelected
        {
            get
            {
                for (int i = 0; i < Selected.Count; i++)
                    if (Selected[i] is Unit) return true;
                return false;
            }
        }

        public bool HasCombatUnitsSelected
        {
            get
            {
                for (int i = 0; i < Selected.Count; i++)
                    if (Selected[i] is Unit u && u.Faction == Faction.Player && u.Data.canAttack) return true;
                return false;
            }
        }

        public bool IsSelected(Entity e) => Selected.Contains(e);

        public void Prune()
        {
            Selected.RemoveAll(e => e == null || e.IsDead);
        }

        public void Set(List<Entity> entities, bool additive)
        {
            if (!additive)
            {
                foreach (var e in Selected) if (e != null) e.SetSelected(false);
                Selected.Clear();
            }
            foreach (var e in entities)
            {
                if (e == null || e.IsDead) continue;
                if (additive && Selected.Contains(e))
                {
                    e.SetSelected(false);
                    Selected.Remove(e);
                    continue;
                }
                if (!Selected.Contains(e))
                {
                    Selected.Add(e);
                    e.SetSelected(true);
                }
            }
            Changed?.Invoke();
        }

        static readonly List<Entity> Tmp = new List<Entity>();
        public void SelectSingle(Entity e, bool additive)
        {
            Tmp.Clear();
            if (e != null) Tmp.Add(e);
            Set(Tmp, additive);
        }

        public void Clear() => Set(new List<Entity>(), false);

        /// <summary>All player units of the same type currently on screen (double-click select).</summary>
        public void SelectAllOfTypeOnScreen(Entity prototype, bool additive)
        {
            if (prototype == null || G.Cam == null) return;
            var cam = G.Cam.Cam;
            var list = new List<Entity>();
            foreach (var e in Entity.All)
            {
                if (e == null || e.IsDead || e.Faction != Faction.Player) continue;
                if (e.IsBuilding != prototype.IsBuilding) continue;
                if (e.DisplayName != prototype.DisplayName) continue;
                var vp = cam.WorldToViewportPoint(e.Position);
                if (vp.z > 0f && vp.x > -0.02f && vp.x < 1.02f && vp.y > -0.02f && vp.y < 1.02f)
                    list.Add(e);
            }
            Set(list, additive);
        }

        public void NotifyDied(Entity e)
        {
            if (Selected.Remove(e)) Changed?.Invoke();
            foreach (var g in _groups.Values) g.Remove(e);
            if (_hovered == e) _hovered = null;
        }

        public void SetHovered(Entity e)
        {
            if (_hovered == e) return;
            if (_hovered != null) _hovered.SetHovered(false);
            _hovered = e;
            if (_hovered != null && !_hovered.IsDead) _hovered.SetHovered(true);
        }

        // ---------- Control groups ----------

        public void AssignGroup(int n)
        {
            Prune();
            _groups[n] = new List<Entity>(Selected);
            if (G.Hud != null) G.Hud.Notify($"Control group {n} set ({Selected.Count})");
        }

        public void RecallGroup(int n, bool additive)
        {
            if (!_groups.TryGetValue(n, out var g)) return;
            g.RemoveAll(e => e == null || e.IsDead);
            if (g.Count == 0) return;
            Set(new List<Entity>(g), additive);
        }

        public Vector3? GroupCenter(int n)
        {
            if (!_groups.TryGetValue(n, out var g)) return null;
            g.RemoveAll(e => e == null || e.IsDead);
            if (g.Count == 0) return null;
            Vector3 sum = Vector3.zero;
            foreach (var e in g) sum += e.Position;
            return sum / g.Count;
        }

        /// <summary>Center of the current selection (for camera jumps).</summary>
        public Vector3? SelectionCenter()
        {
            Prune();
            if (Selected.Count == 0) return null;
            Vector3 sum = Vector3.zero;
            foreach (var e in Selected) sum += e.Position;
            return sum / Selected.Count;
        }

        public void RaiseChanged() => Changed?.Invoke();
    }
}
