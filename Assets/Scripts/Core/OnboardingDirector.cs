using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoidClash
{
    /// <summary>Small, race-aware learning path that reacts to real match progress.</summary>
    public class OnboardingDirector : MonoBehaviour
    {
        static readonly string[] TerranTitles =
            { "SELECT A WORKER", "BUILD MORE SUPPLY", "OPEN PRODUCTION", "TRAIN YOUR FIRST SOLDIER", "COMMAND YOUR ARMY" };
        static readonly string[] BubbleTitles =
            { "SELECT THE BUBBLE NEXUS", "BUILD AN AERATOR", "SPEED UP THE TIDE", "MAKE POISON BUBBLES", "SEND THE BUBBLE WAVE" };
        static readonly string[] DotsTitles =
            { "SELECT THE CORE DOT", "GATHER 12 LOOSE DOTS", "FORM A DOT KITE", "FORM A DOT SPIKE", "MOVE AS A SHAPE ARMY" };

        sealed class Step
        {
            public string Title;
            public string Body;
            public Func<bool> Complete;
        }

        readonly List<Step> _steps = new List<Step>();
        int _index;
        float _tick;
        bool _visible = true;
        bool _finished;
        int _initialArmy;
        int _initialBubbles;
        int _initialKites;
        int _initialSpikes;

        public int CurrentStep => _index;
        public int TotalSteps => _steps.Count;
        public bool IsFinished => _finished;

        public void Init(PlayerRace race)
        {
            _initialArmy = CountCombatUnits();
            _initialBubbles = CountUnits("bubble") + CountUnits("poison_bubble");
            _initialKites = CountUnits("dot_kite");
            _initialSpikes = CountUnits("dot_spike");
            BuildSteps(race);
            ShowCurrent();
        }

        void Update()
        {
            if (_finished || G.Game == null || G.Game.IsOver) return;
            _tick -= Time.unscaledDeltaTime;
            if (_tick > 0f) return;
            _tick = 0.2f;

            if (_index >= _steps.Count || !_steps[_index].Complete()) return;
            if (G.Audio != null) G.Audio.Play("build_done", 0.35f);
            _index++;
            if (_index >= _steps.Count)
            {
                _finished = true;
                if (G.Hud != null)
                {
                    G.Hud.SetGuidance("READY", "FIELD TRAINING COMPLETE", "Build your army, scout, and destroy the enemy command.", _visible);
                    G.Hud.Notify("Field training complete. You are in command.");
                }
                Invoke(nameof(HideFinished), 8f);
                return;
            }
            ShowCurrent();
        }

        public void SetVisible(bool visible)
        {
            _visible = visible;
            if (_finished)
            {
                if (G.Hud != null) G.Hud.SetGuidance("READY", "FIELD TRAINING COMPLETE", "Build your army, scout, and destroy the enemy command.", visible);
                return;
            }
            ShowCurrent();
        }

        void HideFinished()
        {
            _visible = false;
            if (G.Hud != null) G.Hud.SetGuidance("", "", "", false);
        }

        void ShowCurrent()
        {
            if (G.Hud == null || _steps.Count == 0) return;
            int shown = Mathf.Min(_index + 1, _steps.Count);
            var step = _steps[Mathf.Min(_index, _steps.Count - 1)];
            G.Hud.SetGuidance($"{shown} / {_steps.Count}", step.Title, step.Body, _visible);
        }

        void BuildSteps(PlayerRace race)
        {
            _steps.Clear();
            if (race == PlayerRace.Bubble) BuildBubbleSteps();
            else if (race == PlayerRace.Dots) BuildDotsSteps();
            else BuildTerranSteps();
        }

        void BuildTerranSteps()
        {
            Add(TerranTitles[0], "Left-click a worker near your Command Center.", () => SelectionHas<WorkerUnit>());
            Add(TerranTitles[1], "Choose Supply Depot in the worker command card, then place it.", () => HasBuilding("depot"));
            Add(TerranTitles[2], "Use a worker to build a Barracks.", () => HasBuilding("barracks"));
            Add(TerranTitles[3], "Select the Barracks and train any combat unit.", () => CountCombatUnits() > _initialArmy);
            Add(TerranTitles[4], "Select combat units, press A, then click toward the enemy.", HasAttackOrder);
        }

        void BuildBubbleSteps()
        {
            Add(BubbleTitles[0], "The Nexus creates bubbles automatically.", () => SelectionHasId("bubble_core"));
            Add(BubbleTitles[1], "Use the Nexus command card to place an Aerator.", () => HasBuilding("aerator"));
            Add(BubbleTitles[2], "Select an Aerator and buy a Bubble Speed upgrade.", () => G.Bubble != null && G.Bubble.ProductionLevel > 0);
            Add(BubbleTitles[3], "Build a Poison Pool near your gathering bubbles.", () => HasBuilding("poison_pool"));
            Add(BubbleTitles[4], "Select your bubbles, press A, then click toward the enemy.", () => CountBubbles() > _initialBubbles && HasAttackOrder());
        }

        void BuildDotsSteps()
        {
            Add(DotsTitles[0], "Your Core Dot provides income and leads the swarm.", () => SelectionHasId("dot_core"));
            Add(DotsTitles[1], "The Dot Printer creates two Dots every few seconds.", () => G.Dots != null && G.Dots.LooseDotCount(Faction.Player) >= DotsSystem.KiteDotCost);
            Add(DotsTitles[2], "Select your Dots and use Form Kite in the command card.", () => CountUnits("dot_kite") > _initialKites);
            Add(DotsTitles[3], "Gather eight more Dots and form a long-range Spike.", () => CountUnits("dot_spike") > _initialSpikes);
            Add(DotsTitles[4], "Select your shapes, press A, then click toward the enemy.", HasAttackOrder);
        }

        void Add(string title, string body, Func<bool> complete)
            => _steps.Add(new Step { Title = title, Body = body, Complete = complete });

        static bool SelectionHas<T>() where T : Entity
        {
            if (G.Selection == null) return false;
            foreach (var e in G.Selection.Selected) if (e is T && e.Faction == Faction.Player) return true;
            return false;
        }

        static bool SelectionHasId(string id)
        {
            if (G.Selection == null) return false;
            foreach (var e in G.Selection.Selected)
            {
                if (e.Faction != Faction.Player) continue;
                if (e is Unit u && u.Data.id == id) return true;
                if (e is Building b && b.Data.id == id) return true;
            }
            return false;
        }

        static bool HasBuilding(string id)
        {
            foreach (var e in Entity.All)
                if (e is Building b && !b.IsDead && b.Faction == Faction.Player && b.Data.id == id) return true;
            return false;
        }

        static int CountUnits(string id)
        {
            int count = 0;
            foreach (var e in Entity.All)
                if (e is Unit u && !u.IsDead && u.Faction == Faction.Player && u.Data.id == id) count++;
            return count;
        }

        static int CountCombatUnits()
        {
            int count = 0;
            foreach (var e in Entity.All)
                if (e is Unit u && !u.IsDead && u.Faction == Faction.Player && !(u is WorkerUnit) && u.Data.canAttack) count++;
            return count;
        }

        static int CountBubbles() => CountUnits("bubble") + CountUnits("poison_bubble");

        static bool HasAttackOrder()
        {
            foreach (var e in Entity.All)
                if (e is Unit u && !u.IsDead && u.Faction == Faction.Player && u.Data.canAttack &&
                    (u.State == UnitState.AttackMove || u.State == UnitState.Attack)) return true;
            return false;
        }

        public static string[] StepTitlesFor(PlayerRace race)
        {
            var source = race == PlayerRace.Bubble ? BubbleTitles : (race == PlayerRace.Dots ? DotsTitles : TerranTitles);
            return (string[])source.Clone();
        }
    }
}
