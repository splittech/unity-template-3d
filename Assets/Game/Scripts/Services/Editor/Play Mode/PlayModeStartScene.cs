using Game.Services.SceneService;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Services.Editor.PlayMode
{
    /// <summary>
    /// Starts Play Mode from the core scene of <see cref="SceneServiceConfig"/> regardless of the scenes open in the editor.
    /// The open scenes are restored when Play Mode exits. Can be switched off per user via the menu.
    /// </summary>
    [InitializeOnLoad]
    internal static class PlayModeStartScene
    {
        private const string MenuPath = "Game/Play Mode/Start From Boot Scene";
        private const string EnabledPrefKey = "Game.PlayModeStartScene.Enabled";

        private static bool Enabled
        {
            get => EditorPrefs.GetBool(EnabledPrefKey, true);
            set => EditorPrefs.SetBool(EnabledPrefKey, value);
        }

        static PlayModeStartScene()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            // AssetDatabase may not be ready during domain reload, so resolve the scene on the next editor update.
            EditorApplication.delayCall += Apply;
        }

        [MenuItem(MenuPath)]
        private static void Toggle()
        {
            Enabled = !Enabled;
            Apply();
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateToggle()
        {
            Menu.SetChecked(MenuPath, Enabled);
            return true;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // Re-resolve right before entering Play Mode so config edits are picked up.
            if (state == PlayModeStateChange.ExitingEditMode)
                Apply();
        }

        private static void Apply()
        {
            EditorSceneManager.playModeStartScene = Enabled ? FindBootScene() : null;
        }

        private static SceneAsset FindBootScene()
        {
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(SceneServiceConfig)}");

            if (guids.Length == 0)
            {
                Debug.LogWarning($"{nameof(PlayModeStartScene)}: no {nameof(SceneServiceConfig)} found, Play Mode starts from the open scene.");
                return null;
            }

            var config = AssetDatabase.LoadAssetAtPath<SceneServiceConfig>(AssetDatabase.GUIDToAssetPath(guids[0]));

            if (!SceneMetaAsset.Validate(config.CoreScene, out string errorMessage) ||
                !config.CoreScene.SceneReference.TryGetPath(out string scenePath))
            {
                Debug.LogWarning($"{nameof(PlayModeStartScene)}: core scene is not set in '{config.name}'. {errorMessage}");
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
        }
    }
}
