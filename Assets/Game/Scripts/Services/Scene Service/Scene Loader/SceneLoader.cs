using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Services.SceneService
{
    public sealed class SceneLoader
    {
        /// <summary>
        /// Loading scene in unity is considered done at 90%.
        /// </summary>
        private const float UnitySceneReadyProgress = 0.9f;

        public async UniTask LoadSceneAsync(SceneMetaAsset sceneAsset, IProgress<float> progress, CancellationToken ct)
        {
            progress ??= Progress.Create<float>(_ => { });

            AsyncOperation operation = SceneManager.LoadSceneAsync(
                sceneAsset.SceneReference.Name, LoadSceneMode.Additive);

            if (operation == null)
                throw new InvalidOperationException($"Failed to start loading scene '{sceneAsset.SceneReference.Name}'.");

            operation.allowSceneActivation = false;

            try
            {
                while (!operation.isDone)
                {
                    progress.Report(
                        Mathf.Clamp01(operation.progress / UnitySceneReadyProgress));

                    if (operation.progress >= UnitySceneReadyProgress)
                        operation.allowSceneActivation = true;

                    await UniTask.Yield(PlayerLoopTiming.Update, ct);
                }
            }
            finally
            {
                operation.allowSceneActivation = true;
            }

            progress.Report(1f);
        }

        public UniTask UnloadSceneAsync(SceneMetaAsset sceneAsset, IProgress<float> progress, CancellationToken ct)
        {
            progress ??= Progress.Create<float>(_ => { });

            Scene scene = SceneManager.GetSceneByName(sceneAsset.SceneReference.Name);

            if (!scene.IsValid() || !scene.isLoaded)
            {
                progress.Report(1f);
                return UniTask.CompletedTask;
            }

            AsyncOperation operation = SceneManager.UnloadSceneAsync(scene);

            if (operation == null)
            {
                progress.Report(1f);
                return UniTask.CompletedTask;
            }

            return operation.ToUniTask(progress, PlayerLoopTiming.Update, ct);
        }
    }
}