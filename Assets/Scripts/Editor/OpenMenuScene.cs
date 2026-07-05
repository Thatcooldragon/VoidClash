using UnityEditor.SceneManagement;

namespace VoidClash.Editor
{
    /// <summary>Batch helper: leaves MainMenu as the last-open scene so the project
    /// opens ready to press Play.</summary>
    public static class OpenMenuScene
    {
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        }
    }
}
