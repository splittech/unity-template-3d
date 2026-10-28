using Alchemy.Inspector;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Audio;
using VContainer;

namespace Game.Services.AudioService
{
    public sealed class MusicPlayer : MonoBehaviour
    {
        [BoxGroup("Mixer Group")]
        [SerializeField] private AudioMixerGroup _musicMixerGroup;

        private AudioServiceConfig _config;

        private AudioSource _firstAudioSource;
        private AudioSource _secondAudioSource;

        private AudioSource _currentAudioSource;
        private AudioSource _previousAudioSource;

        private Tween _fadeInTween;
        private Tween _fadeOutTween;

        [Inject]
        public void Construct(AudioServiceConfig config)
        {
            _config = config;

            CreateAudioSources();
        }

        public void CreateAudioSources()
        {
            _firstAudioSource = CreateAudioSource(number: 1);
            _secondAudioSource = CreateAudioSource(number: 2);
        }

        private AudioSource CreateAudioSource(int number)
        {
            GameObject audioSourceObject = new($"{name}'s Audio Source ({number})");
            audioSourceObject.transform.SetParent(transform);
            audioSourceObject.SetActive(false);

            AudioSource audioSource = audioSourceObject.AddComponent<AudioSource>();
            audioSource.outputAudioMixerGroup = _musicMixerGroup;
            audioSource.playOnAwake = false;
            audioSource.loop = true;

            return audioSource;
        }

        public void PlayMusic(MusicMetaAsset musicMetaAsset)
        {
            AudioSource lastAudioSource = _currentAudioSource != null ? _currentAudioSource : _previousAudioSource;

            if (lastAudioSource == null || lastAudioSource == _secondAudioSource)
            {
                SwitchCurrentAudioSource(_firstAudioSource);
            }
            else
            {
                SwitchCurrentAudioSource(_secondAudioSource);
            }

            FadeAudioSources(musicMetaAsset);
            EnableAudioSource(_currentAudioSource, musicMetaAsset);
        }

        public void StopMusic()
        {
            SwitchCurrentAudioSource(null);
            FadeAudioSources(null);
        }

        private void SwitchCurrentAudioSource(AudioSource audioSource)
        {
            if (_currentAudioSource != null)
                _previousAudioSource = _currentAudioSource;

            _currentAudioSource = audioSource;
        }

        private void FadeAudioSources(MusicMetaAsset musicMetaAsset)
        {
            _fadeInTween?.Kill();

            if (_currentAudioSource != null)
            {
                _fadeInTween = _currentAudioSource
                    .DOFade(musicMetaAsset.Volume, _config.MusicFadeTime);
            }

            bool isPreviousFadingOut =
                _fadeOutTween.IsActive() &&
                (AudioSource)_fadeOutTween.target == _previousAudioSource;

            if (_previousAudioSource != null && !isPreviousFadingOut)
            {
                _fadeOutTween?.Kill();

                AudioSource audioSourceToDisable = _previousAudioSource;

                _fadeOutTween = _previousAudioSource
                    .DOFade(0f, _config.MusicFadeTime)
                    .OnComplete(() => DisableAudioSource(audioSourceToDisable))
                    .OnKill(() => DisableAudioSource(audioSourceToDisable));
            }
        }

        private void EnableAudioSource(AudioSource audioSource, MusicMetaAsset musicMetaAsset)
        {
            audioSource.clip = musicMetaAsset.AudioClip;
            audioSource.volume = 0;
            audioSource.gameObject.SetActive(true);
            audioSource.Play();
        }

        private void DisableAudioSource(AudioSource audioSource)
        {
            if (audioSource == null)
                return;

            audioSource.Stop();
            audioSource.gameObject.SetActive(false);
        }
    }
}
