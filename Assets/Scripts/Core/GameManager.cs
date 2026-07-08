using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VoidClash
{
    public enum MatchState { Playing, Victory, Defeat }

    /// <summary>Match state, win/lose detection, pause and scene flow.</summary>
    public class GameManager : MonoBehaviour
    {
        public MatchState State { get; private set; } = MatchState.Playing;
        public bool IsPaused { get; private set; }
        public bool IsOver => State != MatchState.Playing;
        public float MatchTime { get; private set; }

        public event Action<MatchState> StateChanged;
        public event Action<bool> PauseChanged;

        bool _started;
        Unit _boss;

        public void StartMatch()
        {
            _started = true;
            State = MatchState.Playing;
            Entity.AnyDied += OnEntityDied;
        }

        /// <summary>Boss missions: victory = the boss dies (razing the base alone won't do it).</summary>
        public void RegisterBoss(Unit boss)
        {
            _boss = boss;
            boss.Health.Died += _ => EndMatch(MatchState.Victory);
        }

        void Update()
        {
            if (!IsPaused && !IsOver) MatchTime += Time.deltaTime;
        }

        void OnEntityDied(Entity e)
        {
            if (!_started || IsOver || !(e is Building)) return;
            // the dying building is still registered; count survivors excluding it
            int player = 0, enemy = 0;
            foreach (var x in Entity.All)
                if (x is Building b && !b.IsDead && x != e)
                {
                    if (b.Faction == Faction.Player) player++;
                    else if (b.Faction == Faction.Enemy) enemy++;
                }
            if (enemy == 0 && _boss == null) EndMatch(MatchState.Victory);
            else if (player == 0) EndMatch(MatchState.Defeat);
        }

        void EndMatch(MatchState result)
        {
            if (IsOver) return;
            State = result;
            if (result == MatchState.Victory) Campaign.NotifyVictory();
            if (G.Audio != null) G.Audio.Play(result == MatchState.Victory ? "victory" : "defeat");
            StateChanged?.Invoke(State);
            Time.timeScale = Application.isBatchMode ? Time.timeScale : 0.2f; // slow-mo finish
        }

        public void TogglePause() => SetPaused(!IsPaused);

        public void SetPaused(bool paused)
        {
            if (IsOver) return;
            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
            PauseChanged?.Invoke(paused);
        }

        public void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("Game");
        }

        public void LoadNextMission()
        {
            var next = Campaign.NextMission(Campaign.Current);
            if (next == null) return;
            Campaign.Current = next;
            Time.timeScale = 1f;
            SceneManager.LoadScene("Game");
        }

        public void QuitToMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }

        public static void QuitApplication()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void OnDestroy()
        {
            Entity.AnyDied -= OnEntityDied;
        }
    }
}
