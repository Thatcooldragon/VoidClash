using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace VoidClash.Editor
{
    /// <summary>Builds the standalone Windows player. Batch:
    /// -executeMethod VoidClash.Editor.BuildGame.Run</summary>
    public static class BuildGame
    {
        [MenuItem("VoidClash/Build Windows EXE")]
        public static void Run()
        {
            // Skybox/Procedural is only referenced via Shader.Find at runtime,
            // so it must be forced into the build.
            AddAlwaysIncludedShader("Skybox/Procedural");

            var options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/Game.unity" },
                locationPathName = "Build/VoidClash.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"VoidClash build succeeded: {report.summary.outputPath} " +
                          $"({report.summary.totalSize / (1024 * 1024)} MB)");
            }
            else
            {
                Debug.LogError($"VoidClash build FAILED: {report.summary.result}, " +
                               $"{report.summary.totalErrors} errors");
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        static void AddAlwaysIncludedShader(string shaderName)
        {
            var shader = Shader.Find(shaderName);
            if (shader == null) { Debug.LogWarning($"Shader not found: {shaderName}"); return; }

            var settings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset").FirstOrDefault();
            if (settings == null) return;
            var so = new SerializedObject(settings);
            var arr = so.FindProperty("m_AlwaysIncludedShaders");
            for (int i = 0; i < arr.arraySize; i++)
                if (arr.GetArrayElementAtIndex(i).objectReferenceValue == shader) return;
            arr.arraySize++;
            arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = shader;
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
        }
    }
}
