using System.Collections.Generic;
using UnityEngine;

namespace VoidClash
{
    /// <summary>Dots prototype loop: a mobile Core Dot powers nearby Dot structures.
    /// Loose Dots do not mine; Printers make them while powered. Big shapes hide the
    /// Core Dot inside themselves, then release it again when destroyed.</summary>
    public class DotsSystem : MonoBehaviour
    {
        readonly Dictionary<Building, float> _printTimers = new Dictionary<Building, float>();
        public const float PowerRange = 12f;
        const float PrintEvery = 5.5f;
        const float CoreMineralsPerSec = 0.55f;
        const int DotSoftCap = 42;
        const int GiantDots = 20;
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

                if (!IsPowered(printer.Faction, printer.Position))
                {
                    _printTimers[printer] = Mathf.Min(PrintEvery, GetTimer(printer));
                    continue;
                }

                float timer = GetTimer(printer) - dt;
                if (timer <= 0f)
                {
                    timer = PrintEvery;
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

        public bool TryFormGiant(List<Entity> selection, out string message)
        {
            var dots = SelectedLooseDots(selection);
            if (dots.Count < GiantDots)
            {
                message = $"Need {GiantDots} loose Dots to form a Giant";
                return false;
            }

            Vector3 center = Average(dots, GiantDots);
            Unit core = FindCore(Faction.Player, center, PowerRange);
            if (core == null)
            {
                message = "Move a Core Dot near the Dots to power the Giant";
                return false;
            }
            if (!HasCompleteBuilding(Faction.Player, "shape_matrix"))
            {
                message = "Build a Shape Matrix to unlock Giant shapes";
                return false;
            }

            var data = G.DB.Unit("dot_giant");
            if (data == null)
            {
                message = "Dot Giant data missing";
                return false;
            }

            ConsumeUnits(dots, GiantDots);
            ConsumeCore(core);
            var giant = UnitFactory.Spawn(data, Faction.Player, center);
            if (giant != null && G.Selection != null) G.Selection.SelectSingle(giant, false);
            message = "Core Dot hid inside a Dot Giant";
            return true;
        }

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

        static void ConsumeUnits(List<Unit> dots, int count)
        {
            count = Mathf.Min(count, dots.Count);
            for (int i = 0; i < count; i++)
                ConsumeSilently(dots[i]);
        }

        static void ConsumeCore(Unit core)
        {
            ConsumeSilently(core);
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
