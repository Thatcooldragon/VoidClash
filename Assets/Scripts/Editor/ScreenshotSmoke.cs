using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace VoidClash.Editor
{
    /// <summary>Dev/CI helper: plays the menu, a skirmish and a campaign mission in the GUI
    /// editor and captures screenshots so rendering can be inspected.
    /// Launch: -executeMethod VoidClash.Editor.ScreenshotSmoke.Run (non-batchmode).</summary>
    [InitializeOnLoad]
    public static class ScreenshotSmoke
    {
        const string Flag = "vc_screenshot_smoke";
        const string PhaseKey = "vc_screenshot_phase";
        const string MarkKey = "vc_screenshot_mark";

        static ScreenshotSmoke()
        {
            if (SessionState.GetBool(Flag, false))
                EditorApplication.update += Tick;
        }

        public static void Run()
        {
            SessionState.SetBool(Flag, true);
            SessionState.SetInt(PhaseKey, 0);
            SessionState.SetFloat(MarkKey, -1f);
            EditorApplication.update += Tick;
            EditorApplication.isPlaying = true;
        }

        /// <summary>Focused capture of the Bubble Lab foam base.</summary>
        public static void RunBubble()
        {
            SessionState.SetBool(Flag, true);
            SessionState.SetInt(PhaseKey, 20);
            SessionState.SetFloat(MarkKey, -1f);
            EditorApplication.update += Tick;
            EditorApplication.isPlaying = true;
        }

        /// <summary>Focused capture of the Dots Lab (dot-cluster shapes).</summary>
        public static void RunDots()
        {
            SessionState.SetBool(Flag, true);
            SessionState.SetInt(PhaseKey, 30);
            SessionState.SetFloat(MarkKey, -1f);
            EditorApplication.update += Tick;
            EditorApplication.isPlaying = true;
        }

        static void Advance(int phase)
        {
            SessionState.SetInt(PhaseKey, phase);
            SessionState.SetFloat(MarkKey, (float)EditorApplication.timeSinceStartup);
        }

        static float Elapsed => (float)EditorApplication.timeSinceStartup - SessionState.GetFloat(MarkKey, -1f);

        /// <summary>Two-step capture that rides the NORMAL render loop (URP does not support
        /// manual Camera.Render, and ScreenCapture stalls when the editor loses focus):
        /// RequestSnap points the main camera at a RenderTexture; a few frames later
        /// TryCompleteSnap reads it back and saves the PNG.
        /// Note: screen-space-overlay UI is not included.</summary>
        static RenderTexture _rt;
        static string _pendingFile;
        static int _requestFrame;

        static bool SnapPending => _pendingFile != null;

        static void RequestSnap(string file)
        {
            var cam = Camera.main;
            if (cam == null) return;
            _rt = new RenderTexture(1600, 900, 24);
            cam.targetTexture = _rt;
            _pendingFile = file;
            _requestFrame = Time.frameCount;
        }

        static void Snap(string file) => RequestSnap(file);

        static void TryCompleteSnap()
        {
            if (_pendingFile == null) return;
            if (Time.frameCount < _requestFrame + 3) return;
            RenderTexture.active = _rt;
            var tex = new Texture2D(_rt.width, _rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, _rt.width, _rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            System.IO.File.WriteAllBytes(_pendingFile, tex.EncodeToPNG());
            var cam = Camera.main;
            if (cam != null) cam.targetTexture = null;
            Object.Destroy(tex);
            _rt.Release();
            Object.Destroy(_rt);
            _rt = null;
            _pendingFile = null;
        }

        static void Tick()
        {
            if (!EditorApplication.isPlaying || !Application.isPlaying) return;
            TryCompleteSnap();

            if (SessionState.GetFloat(MarkKey, -1f) < 0f)
                SessionState.SetFloat(MarkKey, (float)EditorApplication.timeSinceStartup);

            switch (SessionState.GetInt(PhaseKey, 0))
            {
                case 0: // ensure menu, shoot it
                    if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "MainMenu")
                    {
                        Campaign.Current = null;
                        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
                        break;
                    }
                    if (Elapsed > 5f)
                    {
                        ScreenCapture.CaptureScreenshot("shot_menu.png");
                        Campaign.Current = null;
                        UnityEngine.SceneManagement.SceneManager.LoadScene("Game");
                        Advance(1);
                    }
                    break;

                case 1: // free play base — capture only
                    if (Elapsed > 7f)
                    {
                        if (G.Cam != null) G.Cam.Focus(MapBuilder.PlayerBasePos + new Vector3(2f, 0f, 2f));
                        Snap("shot_game_base.png");
                        Advance(10);
                    }
                    break;

                case 10: // give the capture a second to flush, then stage the demo
                    if (Elapsed > 1.5f)
                    {
                        if (G.PlayerBank != null)
                        {
                            G.PlayerBank.AddMinerals(2000);
                            var depot = G.DB.Building("depot");
                            for (int ring = 1; ring <= 4; ring++)
                                for (int i = 0; i < 12; i++)
                                {
                                    var p = MapBuilder.PlayerBasePos + Quaternion.Euler(0f, i * 30f, 0f) * Vector3.forward * (7f + ring * 4f);
                                    p = new Vector3(Mathf.Round(p.x), 0f, Mathf.Round(p.z));
                                    if (G.Placer.IsValidAt(depot, p))
                                    {
                                        var site = G.Placer.PlaceAt(depot, Faction.Player, p);
                                        foreach (var e in Entity.All)
                                            if (e is WorkerUnit w && e.Faction == Faction.Player) { w.CommandBuild(site); goto placed; }
                                    }
                                }
                            placed:
                            // lift the barracks-less base's CC? no — lift demo: spawn a barracks pre-built and lift it
                            var rax = BuildingFactory.Place(G.DB.Building("barracks"), Faction.Player,
                                MapBuilder.PlayerBasePos + new Vector3(10f, 0f, -2f), true);
                            if (rax != null) rax.CommandLiftOff();
                            for (int i = 0; i < 5; i++)
                                UnitFactory.Spawn(G.DB.Unit(i % 2 == 0 ? "soldier" : "ranged"), Faction.Player,
                                    MapBuilder.PlayerBasePos + new Vector3(6f + i * 1.5f, 0f, 8f));
                        }
                        Advance(2);
                    }
                    break;

                case 2: // construction + flying barracks — capture only
                    if (Elapsed > 8f)
                    {
                        if (G.Cam != null) G.Cam.Focus(MapBuilder.PlayerBasePos + new Vector3(4f, 0f, 2f));
                        Snap("shot_game_construction.png");
                        Advance(11);
                    }
                    break;

                case 11: // flush, then jump to the boss mission
                    if (Elapsed > 1.5f)
                    {
                        Campaign.Current = Campaign.Missions[2];
                        UnityEngine.SceneManagement.SceneManager.LoadScene("Game");
                        Advance(3);
                    }
                    break;

                case 3: // mission briefing overlay — UI needs ScreenCapture; also log presence
                    if (Elapsed > 4f)
                    {
                        Debug.Log($"SMOKE briefing present: {GameObject.Find("Briefing") != null}");
                        ScreenCapture.CaptureScreenshot("shot_campaign_briefing.png");
                        Advance(12);
                    }
                    break;

                case 12: // flush, dismiss briefing, reveal the zerg base
                    if (Elapsed > 1.5f)
                    {
                        var briefing = GameObject.Find("Briefing");
                        if (briefing != null) Object.Destroy(briefing);
                        Time.timeScale = 1f;
                        UnitFactory.Spawn(G.DB.Unit("ranged"), Faction.Player, MapBuilder.EnemyBasePos + new Vector3(-11f, 0f, -11f));
                        if (G.Cam != null) G.Cam.Focus(MapBuilder.EnemyBasePos + new Vector3(-3f, 0f, -3f));
                        Advance(4);
                    }
                    break;

                case 4: // zerg base + overlord — capture only
                    if (Elapsed > 4f)
                    {
                        if (G.Cam != null) G.Cam.Focus(MapBuilder.EnemyBasePos + new Vector3(-3f, 0f, -3f));
                        Snap("shot_campaign_boss.png");
                        Advance(5);
                    }
                    break;

                case 5: // final flush, then quit
                    if (Elapsed > 3f)
                    {
                        Campaign.Current = null;
                        SessionState.SetBool(Flag, false);
                        EditorApplication.Exit(0);
                    }
                    break;

                case 20: // enter Bubble Lab
                    if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Game"
                        || SkirmishConfig.Mode != SkirmishMode.BubbleLab)
                    {
                        Campaign.Current = null;
                        SkirmishConfig.Mode = SkirmishMode.BubbleLab;
                        UnityEngine.SceneManagement.SceneManager.LoadScene("Game");
                        Advance(21);
                    }
                    break;

                case 21: // let bubbles stream, then frame the foam base
                    if (Elapsed > 7f)
                    {
                        if (G.Cam != null) G.Cam.Focus(MapBuilder.PlayerBasePos + new Vector3(2f, 0f, 2f));
                        Snap("shot_bubble_lab.png");
                        Advance(22);
                    }
                    break;

                case 22: // flush, then quit
                    if (Elapsed > 2.5f && !SnapPending)
                    {
                        SkirmishConfig.Mode = SkirmishMode.Terran;
                        SessionState.SetBool(Flag, false);
                        EditorApplication.Exit(0);
                    }
                    break;

                case 30: // enter Dots Lab
                    if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Game"
                        || SkirmishConfig.Mode != SkirmishMode.DotsLab)
                    {
                        Campaign.Current = null;
                        SkirmishConfig.Mode = SkirmishMode.DotsLab;
                        UnityEngine.SceneManagement.SceneManager.LoadScene("Game");
                        Advance(31);
                    }
                    break;

                case 31: // let dots print, form a Core Dot + Giant, then frame the base
                    if (Elapsed > 8f)
                    {
                        if (G.Dots != null)
                        {
                            G.PlayerBank.AddMinerals(400);
                            G.Dots.TryFormCoreDot(new System.Collections.Generic.List<Entity>(), out _);
                            G.Dots.TryFormGiant(new System.Collections.Generic.List<Entity>(), out _);
                        }
                        if (G.Cam != null) G.Cam.Focus(MapBuilder.PlayerBasePos + new Vector3(3f, 0f, 3f));
                        Advance(32);
                    }
                    break;

                case 32: // capture after shapes form
                    if (Elapsed > 2.5f)
                    {
                        Snap("shot_dots_lab.png");
                        Advance(33);
                    }
                    break;

                case 33: // flush, then quit
                    if (Elapsed > 2.5f && !SnapPending)
                    {
                        SkirmishConfig.Mode = SkirmishMode.Terran;
                        SessionState.SetBool(Flag, false);
                        EditorApplication.Exit(0);
                    }
                    break;
            }
        }
    }
}
