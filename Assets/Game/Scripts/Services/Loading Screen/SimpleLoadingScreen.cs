using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Services.LoadingScreen
{
    public sealed class SimpleLoadingScreen : GameLoadingScreen
    {
        [Header("Simple Loading Screen")]
        [SerializeField] private Slider _slider;
        [SerializeField] private TMP_Text _progressDescription;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private float _fadeTime = 1f;

        private Tween _fadeTween;

        public override void SetProgressDescription(string description)
        {
            _progressDescription.text = description;
        }

        protected override UniTask ShowAsync(CancellationToken ct)
        {
            return DoFadeAsync(1f, ct);
        }

        protected override UniTask HideAsync(CancellationToken ct)
        {
            return DoFadeAsync(0f, ct);
        }

        protected override void SetVisibleImmediate(bool visible)
        {
            KillFadeAnimation();
            _canvasGroup.alpha = visible ? 1f : 0f;
        }

        protected override void UpdateProgressVisuals(float progress)
        {
            _slider.value = progress;
        }

        private UniTask DoFadeAsync(float endValue, CancellationToken ct)
        {
            KillFadeAnimation();

            // Independent update keeps the fade running while Time.timeScale is 0.
            // SetLink kills the tween when the GameObject is destroyed.
            _fadeTween = _canvasGroup.DOFade(endValue, _fadeTime)
                .SetUpdate(true)
                .SetLink(gameObject);

            return _fadeTween.ToUniTask(cancellationToken: ct);
        }

        private void KillFadeAnimation()
        {
            _fadeTween?.Kill();
            _fadeTween = null;
        }
    }
}
