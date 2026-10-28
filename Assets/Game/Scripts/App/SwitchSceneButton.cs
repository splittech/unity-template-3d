using Cysharp.Threading.Tasks;
using Game.Services.LoadingScreen;
using Game.Services.SceneService;
using R3;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Game.App
{
    public class SwitchSceneButton : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private SceneMetaAsset _sceneToSwitch;
        [SerializeField] private string _switchSceneProgressText;

        private ISceneService _sceneService;
        private IGameLoadingScreen _loadingScreen;

        [Inject]
        public void Construct(IGameLoadingScreen loadingScreen, ISceneService sceneService)
        {
            _sceneService = sceneService;
            _loadingScreen = loadingScreen;
        }

        private void Start()
        {
            _button.OnClickAsObservable()
                .Subscribe(_ => SwitchSceneAsync().Forget())
                .AddTo(this);
        }

        private async UniTask SwitchSceneAsync()
        {
            await _loadingScreen.ShowScreenAsync();

            _loadingScreen.SetProgressDescription(_switchSceneProgressText);
            await _sceneService.SwitchSceneAsync(_sceneToSwitch, _loadingScreen.LoadingProgress);

            await _loadingScreen.HideScreenAsync();
        }
    }
}
