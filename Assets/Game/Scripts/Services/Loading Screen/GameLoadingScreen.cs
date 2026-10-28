using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Services.LoggingService;
using UnityEngine;
using VContainer;

namespace Game.Services.LoadingScreen
{
    public abstract class GameLoadingScreen : MonoBehaviour, IGameLoadingScreen
    {
        private enum State
        {
            Hidden,
            Showing,
            Shown,
            Hiding
        }

        [Header("Game Loading Screen")]
        [SerializeField] private float _minimumShowTime = 1f;

        private IGameLogger _logger;

        private IProgress<float> _progress;
        private float _targetProgressValue;
        private float _progressValue;
        private float _showTime;

        private State _state = State.Hidden;
        private CancellationTokenSource _transitionCts;

        public IProgress<float> LoadingProgress => _progress ??= Progress.Create<float>(v => _targetProgressValue = v);

        [Inject]
        public void Construct(ILoggingService loggingService)
        {
            _logger = loggingService.GetLogger(LoggingChannel.LoadingScreen);
        }

        private void Update()
        {
            // Progress keeps running while hiding: hide waits until the bar reaches 100%.
            if (_state is not (State.Shown or State.Hiding))
                return;

            _showTime += Time.unscaledDeltaTime;

            float timeLimit = _minimumShowTime > 0f ? _showTime / _minimumShowTime : 1f;
            _progressValue = Mathf.Clamp01(Mathf.Min(_targetProgressValue, timeLimit));

            UpdateProgressVisuals(_progressValue);
        }

        private void OnDestroy()
        {
            _transitionCts?.Cancel();
            _transitionCts?.Dispose();
            _transitionCts = null;
        }

        public async UniTask ShowScreenAsync(CancellationToken ct = default)
        {
            if (_state is State.Showing or State.Shown)
                return;

            _logger.Log("Showing loading screen...");

            CancellationToken token = BeginTransition(State.Showing, ct);
            ResetProgress();
            gameObject.SetActive(true);

            if (await ShowAsync(token).SuppressCancellationThrow())
                return;

            _state = State.Shown;
            _logger.Log("Loading screen is shown.");
        }

        public async UniTask HideScreenAsync(CancellationToken ct = default)
        {
            if (_state is State.Hiding or State.Hidden)
                return;

            _logger.Log("Hiding loading screen...");

            CancellationToken token = BeginTransition(State.Hiding, ct);

            // Respect the minimum show time and let the bar reach 100% even if loading never reported 1.
            _targetProgressValue = 1f;

            if (await UniTask.WaitUntil(() => _progressValue >= 1f, cancellationToken: token).SuppressCancellationThrow())
                return;

            if (await HideAsync(token).SuppressCancellationThrow())
                return;

            CompleteHide();
            _logger.Log("Loading screen is hidden.");
        }

        public void ShowScreenImmediate()
        {
            BeginTransition(State.Shown, CancellationToken.None);
            ResetProgress();
            gameObject.SetActive(true);
            SetVisibleImmediate(true);

            _logger.Log("Loading screen is shown.");
        }

        public void HideScreenImmediate()
        {
            BeginTransition(State.Hidden, CancellationToken.None);
            SetVisibleImmediate(false);
            CompleteHide();

            _logger.Log("Loading screen is hidden.");
        }

        /// <summary>
        /// Cancels the running transition and switches to <paramref name="state"/>.
        /// The returned token is cancelled by the next transition, the caller or destruction.
        /// </summary>
        private CancellationToken BeginTransition(State state, CancellationToken ct)
        {
            _transitionCts?.Cancel();
            _transitionCts?.Dispose();
            _transitionCts = CancellationTokenSource.CreateLinkedTokenSource(ct, destroyCancellationToken);

            _state = state;
            return _transitionCts.Token;
        }

        private void ResetProgress()
        {
            _targetProgressValue = 0f;
            _progressValue = 0f;
            _showTime = 0f;

            UpdateProgressVisuals(0f);
        }

        private void CompleteHide()
        {
            _state = State.Hidden;
            gameObject.SetActive(false);
            SetProgressDescription(string.Empty);
        }

        public abstract void SetProgressDescription(string description);
        protected abstract UniTask ShowAsync(CancellationToken ct);
        protected abstract UniTask HideAsync(CancellationToken ct);
        protected abstract void SetVisibleImmediate(bool visible);
        protected abstract void UpdateProgressVisuals(float progress);
    }
}
