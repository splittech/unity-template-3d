using Eflatun.SceneReference;
using UnityEngine;

namespace Game.Services.SceneService
{
    [CreateAssetMenu(fileName = "Scene Meta Asset", menuName = "Game/Meta Assets/Scene Meta Asset", order = 0)]
    public sealed class SceneMetaAsset : ScriptableObject
    {
        public SceneReference SceneReference;
        public bool Initial;
        public bool Persistent;

        public static bool Validate(SceneMetaAsset sceneAsset, out string errorMessage)
        {
            bool validated = true;
            errorMessage = "";

            if (sceneAsset == null)
            {
                errorMessage = $"SceneMetaAsset 'null': Scene asset can't be null.";
                return false;
            }

            if (sceneAsset.SceneReference == null)
            {
                errorMessage = $"SceneMetaAsset '{sceneAsset.name}': Scene asset has no scene reference.";
                validated = false;
            }

            return validated;
        }
    }
}