using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Services.Common;
using Game.Services.LoggingService;

namespace Game.Services.SceneService
{
    public sealed class SceneService : ISceneService
    {
        private readonly SceneServiceConfig _config;
        private readonly SceneLoader _sceneLoader;
        private readonly IGameLogger _logger;

        private readonly HashSet<SceneMetaAsset> _loadedScenes = new();

        public HashSet<SceneMetaAsset> LoadedScenes => _loadedScenes;

        public SceneService(SceneServiceConfig config, ILoggingService loggingService, SceneLoader sceneLoader)
        {
            _config = config;
            _sceneLoader = sceneLoader;

            _logger = loggingService.GetLogger(LoggingChannel.SceneService);

            _loadedScenes.Add(_config.CoreScene);

            ValidateConfig();
        }

        private void ValidateConfig()
        {
            if (_config.Validate(out var errorMessage))
            {
                _logger.Log("Scene validation is successful.");
            }
            else
            {
                _logger.LogError(errorMessage);
            }
        }

        public async UniTask LoadSceneAsync(
            SceneMetaAsset sceneAsset,
            IProgress<float> progress = null,
            CancellationToken ct = default)
        {
            string sceneName = sceneAsset != null ? sceneAsset.name : "null";

            if (!SceneMetaAsset.Validate(sceneAsset, out var errorMessage))
            {
                _logger.LogError("Error while loading a scene: " + errorMessage);
                return;
            }

            if (_loadedScenes.Contains(sceneAsset))
            {
                _logger.LogWarning($"Scene with SceneMetaAsset: '{sceneName}' is already loaded.");
                return;
            }

            _logger.Log($"Loading scene with SceneMetaAsset: '{sceneName}'.");
            _loadedScenes.Add(sceneAsset);

            await _sceneLoader.LoadSceneAsync(sceneAsset, progress, ct);
            progress?.Report(1f);
        }

        public async UniTask UnloadSceneAsync(
            SceneMetaAsset sceneAsset,
            IProgress<float> progress = null,
            CancellationToken ct = default)
        {
            string sceneName = sceneAsset != null ? sceneAsset.name : "null";

            if (!SceneMetaAsset.Validate(sceneAsset, out var errorMessage))
            {
                _logger.LogError("Error while unloading a scene: " + errorMessage);
                return;
            }

            if (!_loadedScenes.Contains(sceneAsset))
            {
                _logger.LogWarning($"Scene with SceneMetaAsset: '{sceneName}' is not loaded.");
                return;
            }

            _logger.Log($"Unloading scene with SceneMetaAsset: '{sceneName}'.");
            _loadedScenes.Remove(sceneAsset);

            await _sceneLoader.UnloadSceneAsync(sceneAsset, progress, ct);
            progress?.Report(1f);
        }

        public UniTask LoadSceneListAsync(
            List<SceneMetaAsset> sceneAssets,
            IProgress<float> progress = null,
            CancellationToken ct = default)
        {
            CompositeProgress compositeProgress = new(value => progress?.Report(value));

            List<UniTask> tasks = sceneAssets
                .Except(_loadedScenes)
                .Select(scene => LoadSceneAsync(scene, compositeProgress.CreateSubProgress(), ct))
                .ToList();

            return UniTask.WhenAll(tasks);
        }

        public UniTask UnloadSceneListAsync(
            List<SceneMetaAsset> sceneAssets,
            IProgress<float> progress = null,
            CancellationToken ct = default)
        {
            CompositeProgress compositeProgress = new(value => progress?.Report(value));

            List<UniTask> tasks = sceneAssets
                .Intersect(_loadedScenes)
                .Select(scene => UnloadSceneAsync(scene, compositeProgress.CreateSubProgress(), ct))
                .ToList();

            return UniTask.WhenAll(tasks);
        }

        public async UniTask SwitchSceneAsync(
            SceneMetaAsset sceneAsset,
            IProgress<float> progress = null,
            CancellationToken ct = default)
        {
            string sceneName = sceneAsset != null ? sceneAsset.name : "null";
            _logger.Log($"Switch scene to scene with SceneMetaAsset '{sceneName}'.");

            if (!SceneMetaAsset.Validate(sceneAsset, out var errorMessage))
            {
                _logger.LogError(errorMessage);
                return;
            }

            CompositeProgress compositeProgress = new(value => progress?.Report(value));
            IProgress<float> unloadProgress = compositeProgress.CreateSubProgress();
            IProgress<float> loadProgress = compositeProgress.CreateSubProgress();

            await UnloadNonpersistentScenesAsync(unloadProgress, ct);
            await LoadSceneAsync(sceneAsset, loadProgress, ct);
            progress?.Report(1f);
        }

        public async UniTask LoadInitialScenesAsync(
            IProgress<float> progress = null,
            CancellationToken ct = default)
        {
            _logger.Log($"Loading initial scenes.");

            List<SceneMetaAsset> scenesToLoad = _config.InitialScenes;
            await LoadSceneListAsync(scenesToLoad, progress, ct);

            progress?.Report(1f);
        }

        public async UniTask UnloadNonpersistentScenesAsync(
            IProgress<float> progress = null,
            CancellationToken ct = default)
        {
            _logger.Log($"Unloading nonpersistent scenes.");

            List<SceneMetaAsset> scenesToUnload = _config.NonPersistentScenes;
            await UnloadSceneListAsync(scenesToUnload, progress, ct);

            progress?.Report(1f);
        }
    }
}