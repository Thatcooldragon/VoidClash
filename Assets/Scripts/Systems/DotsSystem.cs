using System.Collections.Generic;
using UnityEngine;

namespace VoidClash
{
    /// <summary>Dots loop: Printers make loose Dots on their own (no power needed). Dots are
    /// the faction's raw material — spend them at a Shape Matrix to form a Core Dot or a Dot
    /// Giant. Core Dots trickle minerals; a slain Giant releases a Core Dot.</summary>
    public class DotsSystem : MonoBehaviour
    {
        readonly Dictionary<Building, float> _printTimers = new Dictionary<Building, float>();
        public const float PowerRange = 12f;      // Core Dot power aura (visual / lore only now)
        const float PrintEvery = 3.2f;            // faster printing
        const int DotsPerPrint = 2;               // more dots per cycle
        const float CoreMineralsPerSec = 0.55f;
        const int DotSoftCap = 80;                // room to bank dots as currency

        // shape costs, paid in loose Dots
        public const int CoreDotCost = 10;
        public const int GiantDotCost = 25;

        float _mineralFraction;

        void OnEnable() => Entity.AnyDied += OnEntityDied;
        void OnDisable() => Entity.AnyDied -= OnEntityDied;

        void Update()
        {
            if (G.Game == null || G.Game.IsPaused || G.Game.IsOver || G.DB == null) return;
            TickCoreIncome();
            TickPrinters();
        }

        void TickCoreIncome()
        {
            foreach (var e in Entity.All)
            {
                if (!(e is Unit u) || u.IsDead || u.Faction != Faction.Player || u.Data.id != "dot_core") continue;
                _mineralFraction += CoreMineralsPerSec * Time.deltaTime;
            }

            int whole = Mathf.FloorToInt(_mineralFraction);
            if (whole <= 0) return;
            _mineralFraction -= whole;
            G.PlayerBank.AddMinerals(whole);
        }

        void TickPrinters()
        {
            float dt = Time.deltaTime;
            var snapshot = new List<Entity>(Entity.All);
            foreach (var e in snapshot)
            {
                if (!(e is Building printer) || printer.IsDead || !printer.IsComplete || printer.Data.id != "dot_printer")
                    continue;

                // Printers run on their own now — no Core Dot power needed.
                float timer = GetTimer(printer) - dt;
                if (timer <= 0f)
                {
                    timer = PrintEvery;
                    for (int i = 0; i < DotsPerPrint; i++)
                        if (CountLooseDots(printer.Faction) < DotSoftCap) SpawnDot(printer);
                }
                _printTimers[printer] = timer;
            }
        }

        float GetTimer(Building printer)
        {
            return _printTimers.TryGetValue(printer, out float timer) ? timer : 1.2f;
        }

        public static bool IsPowered(Faction faction, Vector3 pos)
        {
            return FindCore(faction, pos, PowerRange) != null;
        }

        /// <summary>Spend loose Dots to form a Core Dot (this is how you make more of them).</summary>
        public bool TryFormCoreDot(List<Entity> selection, out string message)
            => TryFormShape(selection, "dot_core", CoreDotCost, false, out message);

        /// <summary>Spend more loose Dots to form a Dot Giant (needs a Shape Matrix).</summary>
        public bool TryFormGiant(List<Entity> selection, out string message)
            => TryFormShape(selection, "dot_giant", GiantDotCost, true, out message);

        bool TryFormShape(List<Entity> selection, string unitId, int dotCost, bool requireMatrix, out string message)
        {
            var data = G.DB.Unit(unitId);
            if (data == null) { message = $"{unitId} data missing"; return false; }

            if (requireMatrix && !HasCompleteBuilding(Faction.Player, "shape_matrix"))
            {
                message = $"Build a Shape Matrix to form a {data.displayName}";
                return false;
            }

            var loose = LooseDots(Faction.Player);
            if (loose.Count < dotCost)
            {
                message = $"Need {dotCost} Dots to form a {data.displayName} (have {loose.Count})";
                return false;
            }

            // form where the player's selected Dots are, else at the whole swarm's center
            var selDots = SelectedLooseDots(selection);
            Vector3 center = selDots.Count > 0 ? Average(selDots, selDots.Count) : Average(loose, loose.Count);

            ConsumeNearest(loose, center, dotCost);   // eats the Dots closest to the shape
            var shape = UnitFactory.Spawn(data, Faction.Player, center);
            if (shape != null && G.Selection != null) G.Selection.SelectSingle(shape, false);
            message = $"Formed a {data.displayName} from {dotCost} Dots";
            return true;
        }

        static List<Unit> LooseDots(Faction faction)
        {
            var list = new List<Unit>();
            foreach (var e in Entity.All)
                if (e is Unit u && !u.IsDead && u.gameObject.activeInHierarchy && u.Faction == faction && u.Data.id == "dot")
                    list.Add(u);
            return list;
        }

        static void ConsumeNearest(List<Unit> dots, Vector3 center, int count)
        {
            dots.Sort((a, b) => (a.Position - center).sqrMagnitude.CompareTo((b.Position - center).sqrMagnitude));
            count = Mathf.Min(count, dots.Count);
            for (int i = 0; i < count; i++) ConsumeSilently(dots[i]);
        }

        /// <summary>Number of loose Dots a faction currently owns (for UI + costs).</summary>
        public int LooseDotCount(Faction faction) => LooseDots(faction).Count;

        static Unit FindCore(Faction faction, Vector3 pos, float range)
        {
            float range2 = range * range;
            Unit best = null;
            float bestD = float.MaxValue;
            foreach (var e in Entity.All)
            {
                if (!(e is Unit u) || u.IsDead || !u.gameObject.activeInHierarchy) continue;
                if (u.Faction != faction || u.Data.id != "dot_core") continue;
                float d = (u.Position - pos).sqrMagnitude;
                if (d <= range2 && d < bestD) { bestD = d; best = u; }
            }
            return best;
        }

        static List<Unit> SelectedLooseDots(List<Entity> selection)
        {
            var dots = new List<Unit>();
            for (int i = 0; i < selection.Count; i++)
                if (selection[i] is Unit u && u.gameObject.activeInHierarchy && !u.IsDead && u.Faction == Faction.Player && u.Data.id == "dot")
                    dots.Add(u);
            return dots;
        }

        static Vector3 Average(List<Unit> dots, int count)
        {
            Vector3 sum = Vector3.zero;
            count = Mathf.Min(count, dots.Count);
            for (int i = 0; i < count; i++) sum += dots[i].Position;
            return count > 0 ? sum / count : MapBuilder.PlayerBasePos;
        }

        static void ConsumeSilently(Unit unit)
        {
            if (unit == null) return;
            if (G.Selection != null) G.Selection.NotifyDied(unit);
            unit.gameObject.SetActive(false);
            Destroy(unit.gameObject);
        }

        static bool HasCompleteBuilding(Faction faction, string id)
        {
            foreach (var e in Entity.All)
                if (e is Building b && !b.IsDead && b.IsComplete && b.Faction == faction && b.Data.id == id)
                    return true;
            return false;
        }

        void SpawnDot(Building printer)
        {
            var data = G.DB.Unit("dot");
            Vector3 pos = printer.Position + Random.insideUnitSphere * 2.2f;
            pos.y = 0f;
            var dot = UnitFactory.Spawn(data, printer.Faction, pos);
            if (dot == null) return;

            if (printer.RallyPoint.HasValue) dot.CommandAttackMove(printer.RallyPoint.Value);
            else if (printer.Faction == Faction.Enemy) dot.CommandAttackMove(MapBuilder.PlayerBasePos);
            else dot.CommandMove(GatherPoint(printer.Faction));

            if (printer.Faction == Faction.Player && G.Audio != null) G.Audio.Play("deposit", 0.18f);
        }

        static Vector3 GatherPoint(Faction faction)
        {
            Unit core = FindCore(faction, faction == Faction.Player ? MapBuilder.PlayerBasePos : MapBuilder.EnemyBasePos, 999f);
            if (core != null) return core.Position + (Vector3.zero - core.Position).normalized * 3f;
            return faction == Faction.Player ? MapBuilder.PlayerBasePos : MapBuilder.EnemyBasePos;
        }

        int CountLooseDots(Faction faction)
        {
            int count = 0;
            foreach (var e in Entity.All)
                if (e is Unit u && !u.IsDead && u.Faction == faction && u.Data.id == "dot")
                    count++;
            return count;
        }

        void OnEntityDied(Entity e)
        {
            if (!(e is Unit u) || u.Data == null || u.Data.id != "dot_giant") return;
            var core = UnitFactory.Spawn(G.DB.Unit("dot_core"), u.Faction, u.Position + Random.insideUnitSphere * 1.4f);
            if (core != null && u.Faction == Faction.Player && G.Hud != null)
                G.Hud.Notify("The Dot Giant broke apart. The Core Dot escaped.");
        }
    }

    public class DotPulse : MonoBehaviour
    {
        Vector3 _baseScale;
        float _seed;

        void Start()
        {
            _baseScale = transform.localScale;
            _seed = Random.value * 10f;
        }

        void Update()
        {
            float pulse = 1f + Mathf.Sin(Time.time * 7.5f + _seed) * 0.08f;
            transform.localScale = _baseScale * pulse;
            transform.Rotate(0f, 160f * Time.deltaTime, 0f);
        }
    }

    public class DotPowerRing : MonoBehaviour
    {
        Unit _unit;
        GameObject _ring;

        void Start()
        {
            _unit = GetComponentInParent<Unit>();
            _ring = new GameObject("CoreDotPowerRange");
            _ring.name = "CoreDotPowerRange";
            _ring.transform.SetParent(_unit != null ? _unit.transform : transform, false);
            _ring.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            const int segments = 36;
            float circumference = 2f * Mathf.PI * DotsSystem.PowerRange;
            for (int i = 0; i < segments; i++)
            {
                float angle = i * (360f / segments);
                var seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                seg.name = "RangeSegment";
                Destroy(seg.GetComponent<Collider>());
                seg.transform.SetParent(_ring.transform, false);
                seg.transform.localPosition = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * DotsSystem.PowerRange;
                seg.transform.localRotation = Quaternion.Euler(0f, angle, 0f);
                seg.transform.localScale = new Vector3(circumference / segments * 0.55f, 0.035f, 0.08f);
                seg.GetComponent<Renderer>().sharedMaterial = MaterialLibrary.Get("rally");
            }
            _ring.SetActive(false);
        }

        void Update()
        {
            if (_ring == null || _unit == null || G.Selection == null) return;
            _ring.SetActive(!_unit.IsDead && G.Selection.IsSelected(_unit));
        }
    }
}
