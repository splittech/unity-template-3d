using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Game.Services.SceneService
{
    public interface ISceneService
    {
        HashSet<SceneMetaAsset> LoadedScenes { get; }

        UniTask LoadSceneAsync(
            SceneMetaAsset sceneAsset,
            IProgress<float> progress = null,
            CancellationToken ct = default);

        UniTask UnloadSceneAsync(
            SceneMetaAsset sceneAsset,
            IProgress<float> progress = null,
            CancellationToken ct = default);

        UniTask LoadSceneListAsync(
            List<SceneMetaAsset> sceneAssets,
            IProgress<float> progress = null,
            CancellationToken ct = default);

        UniTask UnloadSceneListAsync(
            List<SceneMetaAsset> sceneAssets,
            IProgress<float> progress = null,
            CancellationToken ct = default);

        UniTask SwitchSceneAsync(
            SceneMetaAsset sceneAsset,
            IProgress<float> progress = null,
            CancellationToken ct = default);

        UniTask LoadInitialScenesAsync(
            IProgress<float> progress = null,
            CancellationToken ct = default);

        UniTask UnloadNonpersistentScenesAsync(
            IProgress<float> progress = null,
            CancellationToken ct = default);
    }
}