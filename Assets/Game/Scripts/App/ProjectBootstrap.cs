using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Services.LoadingScreen;
using Game.Services.SceneService;
using UnityEngine;
using VContainer;

namespace Game.App
{
    public class ProjectBootstrap : MonoBehaviour
    {
        [SerializeField] private string _loadingInitialScenesProgressText = "Starting game";

        private IGameLoadingScreen _loadingScreen;
        private ISceneService _sceneService;

        [Inject]
        public void Construct(IGameLoadingScreen loadingScreen, ISceneService sceneService)
        {
            _loadingScreen = loadingScreen;
            _sceneService = sceneService;
        }

        private void Start()
        {
            StartAsync(destroyCancellationToken).Forget();
        }

        public async UniTask StartAsync(CancellationToken ct)
        {
            _loadingScreen.ShowScreenImmediate();

            _loadingScreen.SetProgressDescription(_loadingInitialScenesProgressText);
            await _sceneService.LoadInitialScenesAsync(_loadingScreen.LoadingProgress, ct);

            await _loadingScreen.HideScreenAsync(ct);
        }
    }
}
