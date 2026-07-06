using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VoidClash
{
    /// <summary>The in-game HUD: resources, minimap, selection panel, command card,
    /// notifications, pause + victory/defeat screens, drag-selection rectangle.</summary>
    public class HUD : MonoBehaviour
    {
        Canvas _canvas;
        Text _mineralsText, _supplyText, _timerText, _idleWorkerText, _objectiveText;
        RawImage _minimapImage;
        RectTransform _minimapRT;
        RectTransform _selectionPanel;
        RectTransform _commandCard;
        Text _notifyText;
        float _notifyUntil;
        Image _dragRect;
        RectTransform _pausePanel, _endPanel;
        Text _endTitle;
        Text _endBody;
        Text _tooltipText;
        RectTransform _tooltip;
        float _underAttackCooldown;
        float _refreshTimer;

        readonly List<(KeyCode key, System.Action action)> _hotkeys = new List<(KeyCode, System.Action)>();

        public void Build()
        {
            _canvas = UIFactory.CreateCanvas("HUD", 10);
            BuildTopBar();
            BuildMinimapPanel();
            BuildSelectionPanel();
            BuildCommandCard();
            BuildNotify();
            BuildDragRect();
            BuildTooltip();
            BuildPausePanel();
            BuildEndPanel();

            G.Selection.Changed += OnSelectionChanged;
            G.PlayerBank.Changed += RefreshResources;
            G.Game.PauseChanged += OnPauseChanged;
            G.Game.StateChanged += OnMatchEnded;
            RefreshResources();
            RefreshIdleWorkers();
            OnSelectionChanged();
        }

        void Update()
        {
            if (_timerText != null && G.Game != null)
            {
                int t = (int)G.Game.MatchTime;
                _timerText.text = $"{t / 60:00}:{t % 60:00}";
            }
            if (_notifyText != null && _notifyText.enabled && Time.unscaledTime > _notifyUntil)
                _notifyText.enabled = false;
            if (_minimapImage != null && G.Minimap != null && _minimapImage.texture == null)
                _minimapImage.texture = G.Minimap.Texture;
            if (_underAttackCooldown > 0f) _underAttackCooldown -= Time.unscaledDeltaTime;

            _refreshTimer -= Time.unscaledDeltaTime;
            if (_refreshTimer <= 0f)
            {
                _refreshTimer = 0.35f;
                RefreshIdleWorkers();
                RefreshSelectionPanel(); // live HP / queue readout; card rebuilds only on selection change
            }
        }

        // ---------- Top bar ----------

        void BuildTopBar()
        {
            var bar = UIFactory.Panel(_canvas.transform, "TopBar", UIFactory.PanelColor);
            bar.anchorMin = new Vector2(0f, 1f);
            bar.anchorMax = new Vector2(1f, 1f);
            bar.pivot = new Vector2(0.5f, 1f);
            bar.anchoredPosition = Vector2.zero;
            bar.sizeDelta = new Vector2(0f, 42f);

            var mineralIcon = UIFactory.Panel(bar, "mineralIcon", new Color(0.3f, 0.85f, 1f, 1f));
            UIFactory.SetRect(mineralIcon, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(20f, 20f));
            mineralIcon.rotation = Quaternion.Euler(0, 0, 45f);

            _mineralsText = UIFactory.Label(bar, "minerals", "50", 24);
            UIFactory.SetRect(_mineralsText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(44f, 0f), new Vector2(120f, 30f));

            var supplyIcon = UIFactory.Panel(bar, "supplyIcon", new Color(0.5f, 1f, 0.6f, 1f));
            UIFactory.SetRect(supplyIcon, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(170f, 0f), new Vector2(20f, 20f));

            _supplyText = UIFactory.Label(bar, "supply", "6/10", 24);
            UIFactory.SetRect(_supplyText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(200f, 0f), new Vector2(140f, 30f));

            var idleBtn = UIFactory.TextButton(bar, "idleWorkers", "IDLE 0", 16, SelectIdleWorker);
            UIFactory.SetRect((RectTransform)idleBtn.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(350f, 0f), new Vector2(110f, 30f));
            _idleWorkerText = idleBtn.GetComponentInChildren<Text>();

            _timerText = UIFactory.Label(bar, "timer", "00:00", 22, TextAnchor.MiddleCenter);
            UIFactory.SetRect(_timerText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(120f, 30f));

            var menuBtn = UIFactory.TextButton(bar, "menuBtn", "MENU (Esc)", 16, () => G.Game.TogglePause());
            UIFactory.SetRect((RectTransform)menuBtn.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-12f, 0f), new Vector2(130f, 30f));

            _objectiveText = UIFactory.Label(_canvas.transform, "Objective", "", 19, TextAnchor.MiddleCenter, new Color(0.8f, 0.9f, 1f));
            UIFactory.SetRect(_objectiveText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -58f), new Vector2(960f, 28f));
            RefreshObjective();
        }

        void RefreshResources()
        {
            if (_mineralsText == null) return;
            _mineralsText.text = G.PlayerBank.Minerals.ToString();
            _supplyText.text = $"{G.PlayerBank.SupplyUsed}/{G.PlayerBank.SupplyCap}";
            _supplyText.color = G.PlayerBank.SupplyLeft <= 0 ? new Color(1f, 0.4f, 0.35f) : UIFactory.TextColor;
        }

        void RefreshIdleWorkers()
        {
            if (_idleWorkerText == null) return;
            int idle = CountIdleWorkers();
            _idleWorkerText.text = $"IDLE {idle}";
            _idleWorkerText.color = idle > 0 ? new Color(1f, 0.9f, 0.45f) : UIFactory.TextColor;
        }

        void RefreshObjective()
        {
            if (_objectiveText == null) return;
            if (!Campaign.IsCampaign || Campaign.Current == null || string.IsNullOrEmpty(Campaign.Current.objective))
            {
                _objectiveText.gameObject.SetActive(false);
                return;
            }
            _objectiveText.gameObject.SetActive(true);
            _objectiveText.text = $"Objective: {Campaign.Current.objective}";
        }

        int CountIdleWorkers()
        {
            int count = 0;
            foreach (var e in Entity.All)
                if (e is WorkerUnit w && w.Faction == Faction.Player && !w.IsDead && w.IsIdleForWork)
                    count++;
            return count;
        }

        public void SelectIdleWorker()
        {
            WorkerUnit best = null;
            foreach (var e in Entity.All)
                if (e is WorkerUnit w && w.Faction == Faction.Player && !w.IsDead && w.IsIdleForWork)
                {
                    best = w;
                    break;
                }
            if (best == null)
            {
                Notify("No idle workers");
                if (G.Audio != null) G.Audio.Play("click", 0.35f);
                return;
            }
            G.Selection.SelectSingle(best, false);
            if (G.Cam != null) G.Cam.Focus(best.Position);
            if (G.Audio != null) G.Audio.Play("select", 0.5f);
        }

        // ---------- Minimap ----------

        void BuildMinimapPanel()
        {
            var panel = UIFactory.Panel(_canvas.transform, "MinimapPanel", UIFactory.PanelColor);
            UIFactory.SetRect(panel, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(10f, 10f), new Vector2(250f, 250f));

            var imgGo = new GameObject("minimap");
            imgGo.transform.SetParent(panel, false);
            _minimapImage = imgGo.AddComponent<RawImage>();
            _minimapRT = (RectTransform)imgGo.transform;
            UIFactory.Stretch(_minimapRT, 8f);

            var trigger = imgGo.AddComponent<EventTrigger>();
            void AddEvent(EventTriggerType type)
            {
                var entry = new EventTrigger.Entry { eventID = type };
                entry.callback.AddListener(data => OnMinimapPointer((PointerEventData)data));
                trigger.triggers.Add(entry);
            }
            AddEvent(EventTriggerType.PointerDown);
            AddEvent(EventTriggerType.Drag);
        }

        void OnMinimapPointer(PointerEventData data)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_minimapRT, data.position, data.pressEventCamera, out var local))
            {
                var rect = _minimapRT.rect;
                var uv = new Vector2((local.x - rect.xMin) / rect.width, (local.y - rect.yMin) / rect.height);
                if (uv.x >= 0f && uv.x <= 1f && uv.y >= 0f && uv.y <= 1f)
                    G.Minimap.MoveCameraTo(uv);
            }
        }

        // ---------- Selection panel ----------

        void BuildSelectionPanel()
        {
            _selectionPanel = UIFactory.Panel(_canvas.transform, "SelectionPanel", UIFactory.PanelColor);
            UIFactory.SetRect(_selectionPanel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(560f, 160f));
        }

        void OnSelectionChanged()
        {
            HideTooltip();
            RebuildCommandCard();
            RefreshSelectionPanel();
        }

        void RefreshSelectionPanel()
        {
            if (_selectionPanel == null) return;
            foreach (Transform c in _selectionPanel) Destroy(c.gameObject);

            G.Selection.Prune();
            var sel = G.Selection.Selected;
            _selectionPanel.gameObject.SetActive(sel.Count > 0);
            if (sel.Count == 0) return;

            if (sel.Count == 1) BuildSingleSelection(sel[0]);
            else BuildMultiSelection(sel);
        }

        void BuildSingleSelection(Entity e)
        {
            if (e == null || e.IsDead) return;
            var portrait = UIFactory.Panel(_selectionPanel, "portrait", GetEntityColor(e));
            UIFactory.SetRect(portrait, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(14f, 10f), new Vector2(90f, 90f));
            UIFactory.Label(portrait, "letter", e.DisplayName.Substring(0, 1), 44, TextAnchor.MiddleCenter, Color.white);

            var name = UIFactory.Label(_selectionPanel, "name", e.DisplayName, 24);
            UIFactory.SetRect(name.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(120f, -18f), new Vector2(250f, 30f));

            string hp = e.Health != null ? $"HP  {Mathf.CeilToInt(e.Health.Current)} / {e.Health.Max}" : "";
            var hpText = UIFactory.Label(_selectionPanel, "hp", hp, 20);
            UIFactory.SetRect(hpText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(120f, -52f), new Vector2(250f, 26f));

            string statLine = "";
            if (e is Unit u)
                statLine = e is WorkerUnit worker
                    ? worker.WorkStatus
                    : (u.Data.canAttack ? $"DMG {u.Data.damage}  ({u.Data.damageClass})   RNG {u.Data.attackRange}" : "");
            else if (e is Building b)
            {
                if (!b.IsComplete) statLine = $"Constructing…  {(int)(b.BuildProgress * 100f)}%";
                else if (b.Data.CanAttack) statLine = $"DMG {b.Data.damage}  RNG {b.Data.attackRange}";
                else if (b.Data.supplyProvided > 0) statLine = $"Supply +{b.Data.supplyProvided}";
            }
            var stats = UIFactory.Label(_selectionPanel, "stats", statLine, 17, TextAnchor.MiddleLeft, new Color(0.65f, 0.75f, 0.85f));
            UIFactory.SetRect(stats.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(120f, -84f), new Vector2(360f, 24f));

            // training queue
            if (e is Building bq && bq.Queue.Count > 0)
            {
                for (int i = 0; i < bq.Queue.Count; i++)
                {
                    var slot = UIFactory.Panel(_selectionPanel, $"q{i}", new Color(bq.Queue[i].accentColor.r, bq.Queue[i].accentColor.g, bq.Queue[i].accentColor.b, 0.85f));
                    UIFactory.SetRect(slot, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(120f + i * 46f, 12f), new Vector2(40f, 40f));
                    UIFactory.Label(slot, "l", bq.Queue[i].displayName.Substring(0, 1), 22, TextAnchor.MiddleCenter, Color.white);
                    if (i == 0)
                    {
                        var prog = UIFactory.Panel(slot, "prog", new Color(0f, 0f, 0f, 0.55f));
                        prog.anchorMin = new Vector2(0f, bq.CurrentTrainProgress);
                        prog.anchorMax = Vector2.one;
                        prog.offsetMin = Vector2.zero;
                        prog.offsetMax = Vector2.zero;
                    }
                }
            }
        }

        void BuildMultiSelection(List<Entity> sel)
        {
            int perRow = 11;
            for (int i = 0; i < sel.Count && i < 33; i++)
            {
                var e = sel[i];
                if (e == null || e.IsDead) continue;
                int row = i / perRow, col = i % perRow;
                var slot = UIFactory.Panel(_selectionPanel, $"u{i}", GetEntityColor(e));
                UIFactory.SetRect(slot, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(14f + col * 48f, -12f - row * 48f), new Vector2(42f, 42f));
                UIFactory.Label(slot, "l", e.DisplayName.Substring(0, 1), 20, TextAnchor.MiddleCenter, Color.white);
                // hp sliver
                float frac = e.Health != null ? e.Health.Fraction : 1f;
                var hp = UIFactory.Panel(slot, "hp", frac > 0.6f ? Color.green : (frac > 0.3f ? Color.yellow : Color.red));
                hp.anchorMin = new Vector2(0f, 0f);
                hp.anchorMax = new Vector2(frac, 0.09f);
                hp.offsetMin = Vector2.zero;
                hp.offsetMax = Vector2.zero;

                var btn = slot.gameObject.AddComponent<Button>();
                var captured = e;
                btn.onClick.AddListener(() => G.Selection.SelectSingle(captured, false));
            }
        }

        static Color GetEntityColor(Entity e)
        {
            Color c = e is Unit u ? u.Data.accentColor : (e is Building b ? b.Data.accentColor : Color.gray);
            return new Color(c.r * 0.55f, c.g * 0.55f, c.b * 0.55f, 0.95f);
        }

        // ---------- Command card ----------

        void BuildCommandCard()
        {
            _commandCard = UIFactory.Panel(_canvas.transform, "CommandCard", UIFactory.PanelColor);
            UIFactory.SetRect(_commandCard, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-10f, 10f), new Vector2(330f, 230f));
        }

        void RebuildCommandCard()
        {
            if (_commandCard == null) return;
            foreach (Transform c in _commandCard) Destroy(c.gameObject);
            _hotkeys.Clear();

            var sel = G.Selection.Selected;
            _commandCard.gameObject.SetActive(sel.Count > 0);
            if (sel.Count == 0) return;

            bool hasWorker = false, hasCombat = false;
            Building trainer = null;
            Building liftable = null;
            Building cancelable = null;
            Building bubbleBuilder = null;
            foreach (var e in sel)
            {
                if (e is WorkerUnit && e.Faction == Faction.Player) hasWorker = true;
                else if (e is Unit u && u.Faction == Faction.Player && u.Data.canAttack) hasCombat = true;
                if (e is Building cb && cb.Faction == Faction.Player && !cb.IsComplete && cancelable == null)
                    cancelable = cb;
                if (e is Building b && b.Faction == Faction.Player && b.IsComplete)
                {
                    if (b.Data.CanTrain && !b.IsAirborne && trainer == null) trainer = b;
                    if (b.CanLift && liftable == null) liftable = b;
                    if (b.Data.opensBuildMenu && bubbleBuilder == null) bubbleBuilder = b;
                }
            }

            int slot = 0;
            if (hasWorker)
            {
                BuildHotbar("terran", ref slot);
            }
            else if (bubbleBuilder != null)
            {
                BuildHotbar("bubble", ref slot);
            }
            else if (trainer != null)
            {
                var trainKeys = new[] { KeyCode.Q, KeyCode.W, KeyCode.E };
                var captured = trainer;
                for (int i = 0; i < trainer.Data.trainableUnits.Length && i < trainKeys.Length; i++)
                {
                    var ud = G.DB.Unit(trainer.Data.trainableUnits[i]);
                    if (ud == null) continue;
                    var key = trainKeys[i];
                    AddCommandButton(slot++, $"{ud.displayName}\n<{key}>  {ud.mineralCost}m {ud.supplyCost}s",
                        () => TryTrain(captured, ud), key,
                        $"{ud.displayName} — {ud.mineralCost} minerals, {ud.supplyCost} supply, {ud.trainTime:0}s\n{ud.description}",
                        captured.CanQueue(ud));
                }
            }

            if (cancelable != null)
            {
                var captured = cancelable;
                AddCommandButton(5, "Cancel\n<X>", () => captured.CancelConstruction(), KeyCode.X,
                    "Cancel construction and refund most of the minerals", true);
            }

            if (liftable != null)
            {
                var captured = liftable;
                if (liftable.Flight == Building.FlightState.Grounded)
                    AddCommandButton(5, "Lift Off\n<L>", () => captured.CommandLiftOff(), KeyCode.None,
                        "Lift into the air: mobile but inactive while flying", true);
                else if (liftable.Flight == Building.FlightState.Flying)
                    AddCommandButton(5, "Land\n<L>", () => G.Input.BeginLandMode(), KeyCode.None,
                        "Choose a clear landing zone. Right-click to fly around", true);
            }

            if (hasCombat || hasWorker)
            {
                AddCommandButton(6, "Attack\n<A>", () => G.Input.BeginAttackMoveMode(), KeyCode.None,
                    "Attack-move: units engage everything on the way", hasCombat);
                AddCommandButton(7, "Stop\n<S>", () => IssueSimple(u => u.CommandStop()), KeyCode.None, "Stop all actions", true);
                AddCommandButton(8, "Hold\n<H>", () => IssueSimple(u => u.CommandHold()), KeyCode.None, "Hold position", true);
            }
        }

        /// <summary>Fills the command card with the build buttons for one tech group
        /// ("terran" or "bubble"), so races never see each other's structures.</summary>
        void BuildHotbar(string techGroup, ref int slot)
        {
            for (int i = 0; i < G.DB.buildings.Count; i++)
            {
                var bd = G.DB.buildings[i];
                if (bd.techGroup != techGroup) continue;
                var key = bd.hotkey;
                AddCommandButton(slot++, $"{bd.displayName}\n<{KeyLabel(key)}>  {bd.mineralCost}m",
                    () => G.Input.BeginPlacement(bd), key,
                    $"{bd.displayName} — {bd.mineralCost} minerals, {bd.buildTime:0}s\n{bd.description}",
                    G.PlayerBank.CanAfford(bd.mineralCost));
            }
        }

        void TryTrain(Building b, UnitData ud)
        {
            if (b == null || b.IsDead) return;
            if (!b.TryQueue(ud))
            {
                if (!G.PlayerBank.CanAfford(ud.mineralCost)) Notify("Not enough minerals");
                else if (G.PlayerBank.SupplyLeft < ud.supplyCost) Notify("Supply cap reached — build a Supply Depot");
                else Notify("Queue is full");
                G.Audio.Play("error");
            }
            else
            {
                G.Audio.Play("click", 0.6f);
                RefreshSelectionPanel();
            }
        }

        void IssueSimple(System.Action<Unit> act)
        {
            foreach (var e in G.Selection.Selected)
                if (e is Unit u && u.Faction == Faction.Player) act(u);
        }

        static string KeyLabel(KeyCode key)
        {
            return key == KeyCode.None ? "-" : key.ToString().ToUpperInvariant();
        }

        void AddCommandButton(int slot, string label, System.Action onClick, KeyCode hotkey, string tooltip, bool enabled)
        {
            if (!enabled && !tooltip.Contains("Blocked:"))
                tooltip += "\nBlocked: " + DisabledReason(label);

            int row = slot / 3, col = slot % 3;
            var btn = UIFactory.TextButton(_commandCard, $"cmd{slot}", label, 15, () =>
            {
                onClick();
            }, enabled ? UIFactory.PanelLight : new Color(0.08f, 0.1f, 0.13f, 0.9f));
            var rt = (RectTransform)btn.transform;
            UIFactory.SetRect(rt, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(10f + col * 105f, -10f - row * 72f), new Vector2(100f, 66f));
            btn.interactable = enabled;

            if (hotkey != KeyCode.None)
                _hotkeys.Add((hotkey, onClick));

            // tooltip hover
            var trigger = btn.gameObject.AddComponent<EventTrigger>();
            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ => ShowTooltip(tooltip));
            trigger.triggers.Add(enter);
            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(_ => HideTooltip());
            trigger.triggers.Add(exit);
        }

        static string DisabledReason(string label)
        {
            int minerals = ParseCostBefore(label, 'm');
            int supply = ParseCostBefore(label, 's');
            if (minerals > 0 && G.PlayerBank.Minerals < minerals)
                return $"need {minerals - G.PlayerBank.Minerals} more minerals.";
            if (supply > 0 && G.PlayerBank.SupplyLeft < supply)
                return "build a Supply Depot.";
            if (label.StartsWith("Attack")) return "no selected unit can attack.";
            return "queue is full or selected unit cannot use this command.";
        }

        static int ParseCostBefore(string text, char suffix)
        {
            int suffixIndex = text.IndexOf(suffix);
            if (suffixIndex < 0) return 0;
            int end = suffixIndex - 1;
            while (end >= 0 && char.IsWhiteSpace(text[end])) end--;
            int start = end;
            while (start >= 0 && char.IsDigit(text[start])) start--;
            if (end < 0 || start == end) return 0;
            string raw = text.Substring(start + 1, end - start);
            return int.TryParse(raw, out var value) ? value : 0;
        }

        /// <summary>Called by InputController every frame (build/train hotkeys).</summary>
        public void HandleHotkeys()
        {
            for (int i = 0; i < _hotkeys.Count; i++)
                if (Input.GetKeyDown(_hotkeys[i].key))
                {
                    _hotkeys[i].action();
                    return;
                }
        }

        // ---------- Tooltip ----------

        void BuildTooltip()
        {
            _tooltip = UIFactory.Panel(_canvas.transform, "Tooltip", new Color(0.03f, 0.05f, 0.09f, 0.97f));
            UIFactory.SetRect(_tooltip, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-10f, 250f), new Vector2(330f, 84f));
            _tooltipText = UIFactory.Label(_tooltip, "text", "", 16);
            UIFactory.Stretch(_tooltipText.rectTransform, 10f);
            _tooltip.gameObject.SetActive(false);
        }

        void ShowTooltip(string text)
        {
            if (_tooltip == null) return;
            _tooltipText.text = text;
            _tooltip.gameObject.SetActive(true);
        }

        void HideTooltip()
        {
            if (_tooltip != null) _tooltip.gameObject.SetActive(false);
        }

        // ---------- Notifications ----------

        void BuildNotify()
        {
            _notifyText = UIFactory.Label(_canvas.transform, "Notify", "", 26, TextAnchor.MiddleCenter, new Color(1f, 0.9f, 0.5f));
            UIFactory.SetRect(_notifyText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -90f), new Vector2(900f, 40f));
            _notifyText.enabled = false;
        }

        public void Notify(string message)
        {
            if (_notifyText == null) return;
            _notifyText.text = message;
            _notifyText.enabled = true;
            _notifyUntil = Time.unscaledTime + 2.5f;
        }

        public void NotifyUnderAttack(Vector3 pos)
        {
            if (_underAttackCooldown > 0f) return;
            _underAttackCooldown = 10f;
            Notify("Your base is under attack!");
            G.Audio.Play("error", 0.8f);
        }

        // ---------- Drag rect ----------

        void BuildDragRect()
        {
            var go = new GameObject("DragRect");
            go.transform.SetParent(_canvas.transform, false);
            _dragRect = go.AddComponent<Image>();
            _dragRect.color = new Color(0.3f, 0.8f, 1f, 0.18f);
            _dragRect.raycastTarget = false;
            _dragRect.enabled = false;
        }

        public void SetDragRect(Vector3 screenA, Vector3 screenB)
        {
            if (_dragRect == null) return;
            _dragRect.enabled = true;
            var rt = _dragRect.rectTransform;
            // convert screen px to canvas units
            var scale = _canvas.transform.localScale.x;
            if (scale <= 0f) scale = 1f;
            Vector2 min = Vector2.Min(screenA, screenB) / scale;
            Vector2 max = Vector2.Max(screenA, screenB) / scale;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = Vector2.zero;
            rt.anchoredPosition = min;
            rt.sizeDelta = max - min;
        }

        public void ClearDragRect()
        {
            if (_dragRect != null) _dragRect.enabled = false;
        }

        // ---------- Pause / end panels ----------

        void BuildPausePanel()
        {
            _pausePanel = UIFactory.Panel(_canvas.transform, "PausePanel", new Color(0f, 0f, 0f, 0.7f));
            UIFactory.Stretch(_pausePanel);
            var box = UIFactory.Panel(_pausePanel, "box", UIFactory.PanelColor);
            UIFactory.SetRect(box, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(360f, 380f));
            var title = UIFactory.Label(box, "title", "PAUSED", 34, TextAnchor.MiddleCenter);
            UIFactory.SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(300f, 50f));

            void MenuButton(string label, float y, System.Action act)
            {
                var b = UIFactory.TextButton(box, label, label, 20, act);
                UIFactory.SetRect((RectTransform)b.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(280f, 52f));
            }
            MenuButton("Resume", -110f, () => G.Game.SetPaused(false));
            MenuButton("Restart", -172f, () => G.Game.Restart());
            MenuButton("Main Menu", -234f, () => G.Game.QuitToMenu());
            MenuButton("Quit Game", -296f, GameManager.QuitApplication);
            _pausePanel.gameObject.SetActive(false);
        }

        void OnPauseChanged(bool paused)
        {
            if (_pausePanel != null) _pausePanel.gameObject.SetActive(paused);
        }

        void BuildEndPanel()
        {
            _endPanel = UIFactory.Panel(_canvas.transform, "EndPanel", new Color(0f, 0f, 0f, 0.75f));
            UIFactory.Stretch(_endPanel);
            var box = UIFactory.Panel(_endPanel, "box", UIFactory.PanelColor);
            UIFactory.SetRect(box, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(460f, 360f));
            _endTitle = UIFactory.Label(box, "title", "VICTORY", 52, TextAnchor.MiddleCenter);
            UIFactory.SetRect(_endTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(420f, 70f));

            _endBody = UIFactory.Label(box, "body", "", 19, TextAnchor.MiddleCenter, new Color(0.76f, 0.84f, 0.95f));
            UIFactory.SetRect(_endBody.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -132f), new Vector2(390f, 52f));

            _nextMissionBtn = UIFactory.TextButton(box, "next", "Next Mission", 22, () => G.Game.LoadNextMission());
            UIFactory.SetRect((RectTransform)_nextMissionBtn.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 154f), new Vector2(280f, 54f));
            var restart = UIFactory.TextButton(box, "restart", "Restart", 22, () => G.Game.Restart());
            UIFactory.SetRect((RectTransform)restart.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 90f), new Vector2(280f, 54f));
            var menu = UIFactory.TextButton(box, "mainmenu", "Main Menu", 22, () => G.Game.QuitToMenu());
            UIFactory.SetRect((RectTransform)menu.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(280f, 54f));
            _endPanel.gameObject.SetActive(false);
        }
        UnityEngine.UI.Button _nextMissionBtn;

        void OnMatchEnded(MatchState state)
        {
            if (_endPanel == null) return;
            _endPanel.gameObject.SetActive(true);
            bool victory = state == MatchState.Victory;
            _endTitle.text = victory ? "VICTORY" : "DEFEAT";
            _endTitle.color = victory ? new Color(0.4f, 1f, 0.55f) : new Color(1f, 0.35f, 0.3f);
            if (_endBody != null)
            {
                var m = Campaign.Current;
                _endBody.text = m == null
                    ? (victory ? "Enemy base destroyed." : "Your command was destroyed.")
                    : (victory ? m.victoryText : m.defeatText);
            }
            _nextMissionBtn.gameObject.SetActive(victory && Campaign.HasNextMission);
        }

        // ---------- Mission briefing ----------

        RectTransform _briefingPanel;

        public void ShowBriefing(string title, string body)
        {
            if (Application.isBatchMode) return; // tests run unattended
            _briefingPanel = UIFactory.Panel(_canvas.transform, "Briefing", new Color(0f, 0f, 0f, 0.82f));
            UIFactory.Stretch(_briefingPanel);
            var box = UIFactory.Panel(_briefingPanel, "box", UIFactory.PanelColor);
            UIFactory.SetRect(box, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 460f));
            var t = UIFactory.Label(box, "title", title, 32, TextAnchor.MiddleCenter, new Color(0.5f, 0.85f, 1f));
            UIFactory.SetRect(t.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -46f), new Vector2(700f, 50f));
            var b = UIFactory.Label(box, "body", body, 21, TextAnchor.UpperLeft);
            UIFactory.SetRect(b.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -230f), new Vector2(660f, 280f));
            var start = UIFactory.TextButton(box, "start", "ENGAGE", 24, () =>
            {
                Time.timeScale = 1f;
                Destroy(_briefingPanel.gameObject);
                _briefingPanel = null;
            });
            UIFactory.SetRect((RectTransform)start.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(260f, 56f));
            Time.timeScale = 0f; // hold the world while the commander reads
        }
    }
}
