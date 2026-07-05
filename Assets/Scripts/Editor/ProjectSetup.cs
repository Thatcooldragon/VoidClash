using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace VoidClash.Editor
{
    /// <summary>One-shot project setup: bakes data assets, materials, audio, prefabs,
    /// creates both scenes, wires build settings and URP (SSAO, soft shadows, HDR).
    /// Runs automatically on first project open; also available from the menu.</summary>
    public static class ProjectSetup
    {
        const string DbPath = "Assets/Resources/GameDatabase.asset";

        [InitializeOnLoadMethod]
        static void AutoRun()
        {
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(DbPath.Replace("Assets", Application.dataPath)))
                    Run();
            };
        }

        [MenuItem("VoidClash/Setup Project")]
        public static void Run()
        {
            try
            {
                Debug.Log("VoidClash setup: starting…");
                CreateFolders();
                var db = ScriptableObject.CreateInstance<GameDatabase>();

                BakeData(db);
                BakeMaterials(db);
                BakeAudio(db);

                AssetDatabase.CreateAsset(db, DbPath);
                G.DB = db; // so factories resolve baked materials while baking prefabs
                BakePrefabs(db);

                ConfigureUrp();
                CreateScenes();
                ConfigureBuildSettings();
                CleanupTemplateLeftovers();

                EditorUtility.SetDirty(db);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                if (!Application.isBatchMode)
                    EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");

                Debug.Log("VoidClash setup: complete. Open Assets/Scenes/MainMenu.unity and press Play.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"VoidClash setup FAILED: {ex}");
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }
        }

        static void CreateFolders()
        {
            foreach (var f in new[]
            {
                "Assets/Prefabs", "Assets/Materials", "Assets/Audio",
                "Assets/ScriptableObjects", "Assets/Scenes", "Assets/Resources"
            })
            {
                if (!AssetDatabase.IsValidFolder(f))
                    AssetDatabase.CreateFolder(Path.GetDirectoryName(f).Replace('\\', '/'), Path.GetFileName(f));
            }
        }

        // ---------- Data ----------

        static void BakeData(GameDatabase db)
        {
            foreach (var u in DataDefs.CreateUnits())
            {
                string path = $"Assets/ScriptableObjects/Unit_{u.id}.asset";
                AssetDatabase.DeleteAsset(path);
                AssetDatabase.CreateAsset(u, path);
                db.units.Add(u);
            }
            foreach (var b in DataDefs.CreateBuildings())
            {
                string path = $"Assets/ScriptableObjects/Building_{b.id}.asset";
                AssetDatabase.DeleteAsset(path);
                AssetDatabase.CreateAsset(b, path);
                db.buildings.Add(b);
            }
        }

        // ---------- Materials ----------

        static void BakeMaterials(GameDatabase db)
        {
            foreach (var name in MaterialLibrary.AllNames)
            {
                string path = $"Assets/Materials/{name}.mat";
                AssetDatabase.DeleteAsset(path);
                var mat = MaterialLibrary.Build(name);
                var tex = mat.mainTexture;
                var bump = mat.HasProperty("_BumpMap") ? mat.GetTexture("_BumpMap") : null;
                AssetDatabase.CreateAsset(mat, path);
                bool dirty = false;
                if (tex != null && !AssetDatabase.Contains(tex))
                {
                    // sub-assets must be attached and the property reassigned AFTER CreateAsset,
                    // then re-imported, or the material serializes null texture references
                    AssetDatabase.AddObjectToAsset(tex, mat);
                    mat.mainTexture = tex;
                    if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
                    dirty = true;
                }
                if (bump != null && !AssetDatabase.Contains(bump))
                {
                    AssetDatabase.AddObjectToAsset(bump, mat);
                    mat.SetTexture("_BumpMap", bump);
                    dirty = true;
                }
                if (dirty)
                {
                    EditorUtility.SetDirty(mat);
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                }
                db.materialNames.Add(name);
                db.materials.Add(mat);
            }
            AssetDatabase.SaveAssets();
        }

        // ---------- Audio ----------

        static void BakeAudio(GameDatabase db)
        {
            foreach (var name in SynthLib.AllClipNames)
            {
                string path = $"Assets/Audio/{name}.wav";
                File.WriteAllBytes(path, SynthLib.ToWav(SynthLib.Generate(name)));
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip != null)
                {
                    db.clipNames.Add(name);
                    db.clips.Add(clip);
                }
            }
        }

        // ---------- Prefabs (baked visuals, used by the factories at runtime) ----------

        static void BakePrefabs(GameDatabase db)
        {
            foreach (var u in db.units)
                foreach (var f in new[] { Faction.Player, Faction.Enemy })
                {
                    var temp = new GameObject($"{u.id}_{f}".ToLower());
                    VisualFactory.BuildUnitVisual(temp.transform, u.id, f, u.bodyScale);
                    SavePrefab(temp, $"Assets/Prefabs/Unit_{u.id}_{f}.prefab".ToLower(), db, $"unit_{u.id}_{f}");
                }
            foreach (var b in db.buildings)
                foreach (var f in new[] { Faction.Player, Faction.Enemy })
                {
                    var temp = new GameObject($"{b.id}_{f}".ToLower());
                    VisualFactory.BuildBuildingVisual(temp.transform, b.id, f);
                    SavePrefab(temp, $"Assets/Prefabs/Building_{b.id}_{f}.prefab".ToLower(), db, $"building_{b.id}_{f}");
                }
        }

        static void SavePrefab(GameObject temp, string path, GameDatabase db, string key)
        {
            AssetDatabase.DeleteAsset(path);
            var prefab = PrefabUtility.SaveAsPrefabAsset(temp, path);
            UnityEngine.Object.DestroyImmediate(temp);
            db.visualPrefabNames.Add(key);
            db.visualPrefabs.Add(prefab);
        }

        // ---------- URP ----------

        static void ConfigureUrp()
        {
            var rp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
            var rendererData = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>("Assets/Settings/PC_Renderer.asset");
            if (rp == null || rendererData == null)
            {
                Debug.LogWarning("VoidClash setup: template URP assets not found, skipping URP tuning.");
                return;
            }

            // quality-of-life pipeline settings
            var so = new SerializedObject(rp);
            SetIfExists(so, "m_SupportsHDR", p => p.boolValue = true);
            SetIfExists(so, "m_SoftShadowsSupported", p => p.boolValue = true);
            SetIfExists(so, "m_MainLightShadowsSupported", p => p.boolValue = true);
            SetIfExists(so, "m_ShadowDistance", p => p.floatValue = 90f);
            SetIfExists(so, "m_MainLightShadowmapResolution", p => p.intValue = 2048);
            so.ApplyModifiedPropertiesWithoutUndo();

            // make PC pipeline the default everywhere
            GraphicsSettings.defaultRenderPipeline = rp;
            int currentLevel = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = rp;
            }
            QualitySettings.SetQualityLevel(currentLevel, false);

            AddSsao(rendererData);
            EditorUtility.SetDirty(rp);
            EditorUtility.SetDirty(rendererData);
        }

        static void SetIfExists(SerializedObject so, string prop, Action<SerializedProperty> set)
        {
            var p = so.FindProperty(prop);
            if (p != null) set(p);
        }

        static void AddSsao(ScriptableRendererData rendererData)
        {
            try
            {
                foreach (var f in rendererData.rendererFeatures)
                    if (f != null && f.GetType().Name == "ScreenSpaceAmbientOcclusion")
                        return; // already added

                var ssaoType = Type.GetType(
                    "UnityEngine.Rendering.Universal.ScreenSpaceAmbientOcclusion, Unity.RenderPipelines.Universal.Runtime");
                if (ssaoType == null)
                {
                    Debug.LogWarning("VoidClash setup: SSAO type not found, skipping AO.");
                    return;
                }
                var feature = (ScriptableRendererFeature)ScriptableObject.CreateInstance(ssaoType);
                feature.name = "ScreenSpaceAmbientOcclusion";
                AssetDatabase.AddObjectToAsset(feature, rendererData);

                var so = new SerializedObject(rendererData);
                var list = so.FindProperty("m_RendererFeatures");
                list.arraySize++;
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = feature;
                var map = so.FindProperty("m_RendererFeatureMap");
                if (map != null)
                {
                    map.arraySize++;
                    map.GetArrayElementAtIndex(map.arraySize - 1).longValue = feature.GetInstanceID();
                }
                so.ApplyModifiedPropertiesWithoutUndo();

                // tune intensity/radius
                var fso = new SerializedObject(feature);
                var settings = fso.FindProperty("m_Settings");
                if (settings != null)
                {
                    var intensity = settings.FindPropertyRelative("Intensity");
                    if (intensity != null) intensity.floatValue = 1.5f;
                    var radius = settings.FindPropertyRelative("Radius");
                    if (radius != null) radius.floatValue = 0.4f;
                    fso.ApplyModifiedPropertiesWithoutUndo();
                }
                Debug.Log("VoidClash setup: SSAO renderer feature added.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"VoidClash setup: could not add SSAO feature ({ex.Message}). Continuing without AO.");
            }
        }

        // ---------- Scenes & build settings ----------

        static void CreateScenes()
        {
            var menu = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var menuGo = new GameObject("MenuBootstrap");
            menuGo.AddComponent<MenuBootstrap>();
            EditorSceneManager.SaveScene(menu, "Assets/Scenes/MainMenu.unity");

            var game = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var gameGo = new GameObject("GameBootstrap");
            gameGo.AddComponent<GameBootstrap>();
            EditorSceneManager.SaveScene(game, "Assets/Scenes/Game.unity");
        }

        static void ConfigureBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/Game.unity", true),
            };
            PlayerSettings.productName = "VoidClash";
        }

        static void CleanupTemplateLeftovers()
        {
            AssetDatabase.DeleteAsset("Assets/Scenes/SampleScene.unity");
        }
    }
}
