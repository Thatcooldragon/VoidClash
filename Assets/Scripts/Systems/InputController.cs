using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace VoidClash
{
    /// <summary>All mouse/keyboard game input: click select, drag box, right-click commands,
    /// A/S/H, control groups, build placement mode, pause.</summary>
    public class InputController : MonoBehaviour
    {
        public enum Mode { Normal, AttackMoveTarget, Placement, LandTarget }
        public Mode CurrentMode { get; private set; } = Mode.Normal;

        Vector3 _dragStartScreen;
        bool _dragging;
        bool _mouseDownValid;
        float _lastClickTime;
        Entity _lastClicked;
        float _lastGroupTapTime;
        int _lastGroupTapped = -1;

        const float DragThreshold = 10f;
        int _selectableMask;
        int _groundMask;

        void Start()
        {
            _selectableMask = LayerMask.GetMask("Units", "Buildings", "Minerals");
            _groundMask = LayerMask.GetMask("Ground");
        }

        void Update()
        {
            if (G.Game == null || G.Cam == null || G.Cam.Cam == null) return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (CurrentMode == Mode.Placement) { G.Placer.Cancel(); CurrentMode = Mode.Normal; return; }
                if (CurrentMode == Mode.AttackMoveTarget || CurrentMode == Mode.LandTarget) { CurrentMode = Mode.Normal; return; }
                G.Game.TogglePause();
                return;
            }

            if (G.Game.IsPaused || G.Game.IsOver) return;

            if (CurrentMode == Mode.Placement)
            {
                G.Placer.Tick();
                if (!G.Placer.IsActive) CurrentMode = Mode.Normal;
                return;
            }

            HandleHover();
            HandleMouse();
            HandleKeys();
        }

        bool PointerOverUI => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        Entity RaycastEntity(out RaycastHit hit)
        {
            var ray = G.Cam.Cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit, 500f, _selectableMask))
            {
                var e = hit.collider.GetComponentInParent<Entity>();
                if (e != null && e.Faction == Faction.Enemy && !e.VisibleToPlayer) return null;
                return e;
            }
            return null;
        }

        MineralNode RaycastMineral()
        {
            var ray = G.Cam.Cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, 500f, LayerMask.GetMask("Minerals")))
                return hit.collider.GetComponentInParent<MineralNode>();
            return null;
        }

        bool RaycastGround(out Vector3 pos)
        {
            pos = Vector3.zero;
            var ray = G.Cam.Cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, 500f, _groundMask)) { pos = hit.point; return true; }
            return G.Cam.ScreenToGround(Input.mousePosition, out pos);
        }

        void HandleHover()
        {
            if (_dragging || PointerOverUI) { G.Selection.SetHovered(null); return; }
            var e = RaycastEntity(out _);
            G.Selection.SetHovered(e);
        }

        void HandleMouse()
        {
            // ----- landing-spot click -----
            if (CurrentMode == Mode.LandTarget)
            {
                if (Input.GetMouseButtonDown(0) && !PointerOverUI)
                {
                    if (RaycastGround(out var spot))
                    {
                        spot = BuildingPlacer.SnapToBuildGrid(spot);
                        bool any = false;
                        foreach (var e in G.Selection.Selected)
                            if (e is Building fb && fb.Faction == Faction.Player && fb.IsAirborne)
                            {
                                if (G.Placer.IsValidAt(fb.Data, spot))
                                {
                                    fb.CommandLandAt(spot);
                                    if (G.Effects != null) G.Effects.SpawnMoveMarker(spot, false);
                                    any = true;
                                    break; // one pad per click
                                }
                                if (G.Hud != null) G.Hud.Notify(G.Placer != null ? G.Placer.InvalidReason : "Cannot land there");
                                if (G.Audio != null) G.Audio.Play("error");
                                any = true;
                                break;
                            }
                        if (!any && G.Hud != null) G.Hud.Notify("No airborne building selected");
                    }
                    CurrentMode = Mode.Normal;
                }
                else if (Input.GetMouseButtonDown(1)) CurrentMode = Mode.Normal;
                return;
            }

            // ----- attack-move click -----
            if (CurrentMode == Mode.AttackMoveTarget)
            {
                if (Input.GetMouseButtonDown(0) && !PointerOverUI)
                {
                    var target = RaycastEntity(out _);
                    if (target != null && target.Faction == Faction.Enemy) IssueAttack(target);
                    else if (RaycastGround(out var pos)) IssueAttackMove(pos);
                    CurrentMode = Mode.Normal;
                }
                else if (Input.GetMouseButtonDown(1)) CurrentMode = Mode.Normal;
                return;
            }

            // ----- left: select / drag box -----
            if (Input.GetMouseButtonDown(0))
            {
                _mouseDownValid = !PointerOverUI;
                _dragStartScreen = Input.mousePosition;
                _dragging = false;
            }

            if (Input.GetMouseButton(0) && _mouseDownValid)
            {
                if (!_dragging && (Input.mousePosition - _dragStartScreen).magnitude > DragThreshold)
                    _dragging = true;
                if (_dragging && G.Hud != null)
                    G.Hud.SetDragRect(_dragStartScreen, Input.mousePosition);
            }

            if (Input.GetMouseButtonUp(0) && _mouseDownValid)
            {
                bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                if (_dragging)
                {
                    if (G.Hud != null) G.Hud.ClearDragRect();
                    BoxSelect(_dragStartScreen, Input.mousePosition, shift);
                    _dragging = false;
                }
                else ClickSelect(shift);
                _mouseDownValid = false;
            }

            // ----- right: context command -----
            if (Input.GetMouseButtonDown(1) && !PointerOverUI)
                RightClickCommand();
        }

        void ClickSelect(bool shift)
        {
            var e = RaycastEntity(out _);
            bool isDouble = e != null && e == _lastClicked && Time.unscaledTime - _lastClickTime < 0.32f;
            _lastClicked = e;
            _lastClickTime = Time.unscaledTime;

            if (e == null) { if (!shift) G.Selection.Clear(); return; }

            if (isDouble && e.Faction == Faction.Player)
            {
                G.Selection.SelectAllOfTypeOnScreen(e, shift);
            }
            else
            {
                G.Selection.SelectSingle(e, shift);
            }
            if (e.Faction == Faction.Player && G.Audio != null)
            {
                G.Audio.Play("select", 0.5f);
                G.Audio.PlayVoice("voice_select", 0.55f);
            }
        }

        void BoxSelect(Vector3 a, Vector3 b, bool shift)
        {
            var min = Vector3.Min(a, b);
            var max = Vector3.Max(a, b);
            var cam = G.Cam.Cam;
            var picked = new List<Entity>();
            bool anyUnit = false;
            foreach (var e in Entity.All)
            {
                if (e == null || e.IsDead || e.Faction != Faction.Player) continue;
                var sp = cam.WorldToScreenPoint(e.Position);
                if (sp.z < 0f || sp.x < min.x || sp.x > max.x || sp.y < min.y || sp.y > max.y) continue;
                picked.Add(e);
                if (!e.IsBuilding) anyUnit = true;
            }
            // units take priority over buildings in a mixed box
            if (anyUnit) picked.RemoveAll(e => e.IsBuilding);
            G.Selection.Set(picked, shift);
            if (picked.Count > 0 && G.Audio != null)
            {
                G.Audio.Play("select", 0.5f);
                G.Audio.PlayVoice("voice_select", 0.55f);
            }
        }

        void RightClickCommand()
        {
            G.Selection.Prune();
            var sel = G.Selection.Selected;
            if (sel.Count == 0) return;

            var targetEntity = RaycastEntity(out _);
            var mineral = RaycastMineral();
            RaycastGround(out var groundPos);

            if (targetEntity is Building ownSite && ownSite.Faction == Faction.Player && !ownSite.IsComplete)
            {
                bool anyWorker = false;
                foreach (var e in sel)
                    if (e is WorkerUnit w && w.Faction == Faction.Player)
                    {
                        w.CommandBuild(ownSite);
                        anyWorker = true;
                    }
                if (anyWorker)
                {
                    if (G.Effects != null) G.Effects.SpawnMoveMarker(ownSite.Position, false);
                    if (G.Audio != null) G.Audio.Play("build_place", 0.55f);
                    if (G.Audio != null) G.Audio.PlayVoice("voice_build");
                    if (G.Hud != null) G.Hud.Notify($"Constructing {ownSite.DisplayName}");
                    return;
                }
            }

            // production building selected → set rally
            bool onlyBuildings = true;
            foreach (var e in sel) if (!e.IsBuilding) { onlyBuildings = false; break; }
            if (onlyBuildings)
            {
                bool flew = false;
                foreach (var e in sel)
                    if (e is Building fb && fb.Faction == Faction.Player && fb.IsAirborne)
                    {
                        fb.CommandFlyTo(groundPos);
                        flew = true;
                    }
                if (flew)
                {
                    if (G.Effects != null) G.Effects.SpawnMoveMarker(groundPos, false);
                    if (G.Audio != null) G.Audio.Play("move", 0.5f);
                    return;
                }
                foreach (var e in sel)
                    if (e is Building b && b.Faction == Faction.Player && b.Data.CanTrain)
                    {
                        Vector3 rally = mineral != null ? mineral.transform.position : groundPos;
                        b.SetRally(rally);
                        if (G.Hud != null) G.Hud.Notify("Rally point set");
                    }
                return;
            }

            if (targetEntity != null && targetEntity.Faction == Faction.Enemy)
            {
                IssueAttack(targetEntity);
                return;
            }

            if (mineral != null && !mineral.Depleted)
            {
                bool anyWorker = false;
                foreach (var e in sel)
                    if (e is WorkerUnit w && w.Faction == Faction.Player) { w.CommandHarvest(mineral); anyWorker = true; }
                if (anyWorker)
                {
                    if (G.Effects != null) G.Effects.SpawnMoveMarker(mineral.transform.position, false);
                    if (G.Audio != null) G.Audio.Play("move", 0.5f);
                    // non-workers just move there
                    foreach (var e in sel)
                        if (e is Unit u && !(e is WorkerUnit)) u.CommandMove(mineral.transform.position);
                    return;
                }
            }

            IssueMove(groundPos);
        }

        void IssueMove(Vector3 pos)
        {
            var units = PlayerUnitsInSelection();
            if (units.Count == 0) return;
            var spots = FormationSpots(pos, units.Count);
            for (int i = 0; i < units.Count; i++) units[i].CommandMove(spots[i]);
            if (G.Effects != null) G.Effects.SpawnMoveMarker(pos, false);
            if (G.Audio != null) G.Audio.Play("move", 0.5f);
            if (G.Audio != null) G.Audio.PlayVoice("voice_move");
        }

        void IssueAttackMove(Vector3 pos)
        {
            var units = PlayerUnitsInSelection();
            if (units.Count == 0) return;
            var spots = FormationSpots(pos, units.Count);
            for (int i = 0; i < units.Count; i++) units[i].CommandAttackMove(spots[i]);
            if (G.Effects != null) G.Effects.SpawnMoveMarker(pos, true);
            if (G.Audio != null) G.Audio.Play("attack_order", 0.6f);
            if (G.Audio != null) G.Audio.PlayVoice("voice_attack");
        }

        void IssueAttack(Entity target)
        {
            var units = PlayerUnitsInSelection();
            foreach (var u in units)
            {
                if (u.Data.canAttack) u.CommandAttack(target);
                else u.CommandMove(target.Position);
            }
            if (units.Count > 0)
            {
                if (G.Effects != null) G.Effects.SpawnMoveMarker(target.Position, true);
                if (G.Audio != null) G.Audio.Play("attack_order", 0.6f);
                if (G.Audio != null) G.Audio.PlayVoice("voice_attack");
            }
        }

        List<Unit> PlayerUnitsInSelection()
        {
            var list = new List<Unit>();
            foreach (var e in G.Selection.Selected)
                if (e is Unit u && u.Faction == Faction.Player && !u.IsDead) list.Add(u);
            return list;
        }

        static List<Vector3> FormationSpots(Vector3 center, int count)
        {
            var spots = new List<Vector3> { center };
            if (count == 1) return spots;
            int ring = 1;
            while (spots.Count < count)
            {
                int perRing = ring * 6;
                float radius = ring * 1.4f;
                for (int i = 0; i < perRing && spots.Count < count; i++)
                {
                    float ang = i * (360f / perRing);
                    spots.Add(center + Quaternion.Euler(0f, ang, 0f) * Vector3.forward * radius);
                }
                ring++;
            }
            return spots;
        }

        void HandleKeys()
        {
            bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            // control groups
            for (int n = 1; n <= 9; n++)
            {
                var key = KeyCode.Alpha0 + n;
                if (!Input.GetKeyDown(key)) continue;
                if (ctrl) G.Selection.AssignGroup(n);
                else
                {
                    bool doubleTap = _lastGroupTapped == n && Time.unscaledTime - _lastGroupTapTime < 0.35f;
                    G.Selection.RecallGroup(n, shift);
                    if (doubleTap)
                    {
                        var c = G.Selection.SelectionCenter();
                        if (c.HasValue) G.Cam.Focus(c.Value);
                    }
                    _lastGroupTapped = n;
                    _lastGroupTapTime = Time.unscaledTime;
                }
            }

            if (Input.GetKeyDown(KeyCode.F1) && G.Hud != null)
            {
                G.Hud.SelectIdleWorker();
                return;
            }

            if (G.Selection.HasCombatUnitsSelected && Input.GetKeyDown(KeyCode.A))
            {
                CurrentMode = Mode.AttackMoveTarget;
                if (G.Hud != null) G.Hud.Notify("Attack-move: choose target");
            }

            if (G.Selection.HasUnitsSelected)
            {
                if (Input.GetKeyDown(KeyCode.S))
                {
                    foreach (var e in G.Selection.Selected) if (e is Unit u && u.Faction == Faction.Player) u.CommandStop();
                    if (G.Audio != null) G.Audio.Play("move", 0.4f);
                }
                if (Input.GetKeyDown(KeyCode.H))
                {
                    foreach (var e in G.Selection.Selected) if (e is Unit u && u.Faction == Faction.Player) u.CommandHold();
                    if (G.Audio != null) G.Audio.Play("move", 0.4f);
                }
            }

            // L = lift off / land for capable buildings
            if (Input.GetKeyDown(KeyCode.L))
            {
                foreach (var e in G.Selection.Selected)
                    if (e is Building b && b.Faction == Faction.Player && b.CanLift)
                    {
                        if (b.Flight == Building.FlightState.Grounded) b.CommandLiftOff();
                        else if (b.Flight == Building.FlightState.Flying) BeginLandMode();
                        break;
                    }
            }

            // building/train hotkeys come from the HUD command card
            if (G.Hud != null) G.Hud.HandleHotkeys();
        }

        public void BeginPlacement(BuildingData data)
        {
            if (G.Placer.Begin(data))
            {
                CurrentMode = Mode.Placement;
                if (G.Audio != null) G.Audio.PlayVoice("voice_build");
            }
        }

        public void BeginAttackMoveMode()
        {
            if (G.Selection.HasCombatUnitsSelected) CurrentMode = Mode.AttackMoveTarget;
        }

        public void BeginLandMode()
        {
            CurrentMode = Mode.LandTarget;
            if (G.Hud != null) G.Hud.Notify("Choose a landing zone");
        }
    }
}
