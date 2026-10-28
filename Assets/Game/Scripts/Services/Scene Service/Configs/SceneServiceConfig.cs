using System.Collections.Generic;
using System.Linq;
using Alchemy.Inspector;
using Eflatun.SceneReference;
using UnityEngine;

namespace Game.Services.SceneService
{
    [CreateAssetMenu(fileName = "Scene Service Config", menuName = "Game/Configs/Scene Service Config", order = 0)]
    public sealed class SceneServiceConfig : ScriptableObject
    {
        [BoxGroup("Core Scene")]
        public SceneMetaAsset CoreScene;

        [BoxGroup("Other Scenes")]
        public List<SceneMetaAsset> OtherScenes;

        public List<SceneMetaAsset> InitialScenes => OtherScenes.Where(scene => scene.Initial).ToList();
        public List<SceneMetaAsset> NonPersistentScenes => OtherScenes.Where(scene => !scene.Persistent).ToList();

        [BoxGroup("Validate Scenes")]
        [Button]
        private void ValidateScenes()
        {
            if (Validate(out var errorMessage))
            {
                Debug.Log("Validation Successful.");
            }
            else
            {
                Debug.LogError(errorMessage);
            }
        }

        public bool Validate(out string errorMessage)
        {
            bool validated = true;
            errorMessage = "";

            if (OtherScenes == null)
            {
                errorMessage = "\nOther scenes list is null.";
                return false;
            }

            if (CoreScene != null && OtherScenes.Contains(CoreScene))
            {
                errorMessage += "\nCore scene must not be listed in other scenes.";
                validated = false;
            }

            List<SceneMetaAsset> allScenes = OtherScenes.Append(CoreScene).ToList();

            // Different meta assets may reference the same scene, so compare scene GUIDs.
            bool hasDuplicates = allScenes
                .Where(scene => scene != null && scene.SceneReference != null
                    && scene.SceneReference.UnsafeReason != SceneReferenceUnsafeReason.Empty)
                .GroupBy(scene => scene.SceneReference.Guid)
                .Any(group => group.Count() > 1);

            if (hasDuplicates)
            {
                errorMessage += "\nScene list contains duplicate scenes.";
                validated = false;
            }

            foreach (var scene in allScenes)
            {
                if (!SceneMetaAsset.Validate(scene, out var sceneMessage))
                {
                    errorMessage += "\n" + sceneMessage;
                    validated = false;
                }
            }

            return validated;
        }
    }
}
