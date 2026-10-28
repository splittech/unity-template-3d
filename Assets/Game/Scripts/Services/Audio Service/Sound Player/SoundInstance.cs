using System;
using UnityEngine;

namespace Game.Services.AudioService
{
    public enum SoundState
    {
        Pending,
        Playing,
        Paused,
        Stopped,
        Released
    }

    public class SoundInstance
    {
        private readonly AudioSource _audioSource;
        private readonly SoundPool _pool;

        private SoundState _state;
        private SoundMetaAsset _soundMetaAsset;

        public SoundState State => _state;
        public SoundMetaAsset SoundMetaAsset => _soundMetaAsset;
        internal AudioSource AudioSource => _audioSource;

        public SoundInstance(AudioSource audioSource, SoundPool pool)
        {
            _audioSource = audioSource;
            _pool = pool;

            _state = SoundState.Pending;

            _audioSource.gameObject.SetActive(true);
            SetSpatial(false);
        }

        public void Release()
        {
            if (_state == SoundState.Released)
                return;

            _pool.Release(this);
            _state = SoundState.Released;

            _audioSource.Stop();
            _audioSource.clip = null;
            _audioSource.gameObject.SetActive(false);
            _audioSource.transform.position = Vector3.zero;
            _audioSource.pitch = 1f;
        }

        public void Play()
        {
            CheckIsNotReleased();

            if (_state == SoundState.Playing)
                return;

            _state = SoundState.Playing;

            _audioSource.Play();
        }

        public void Stop()
        {
            if (_state is not (SoundState.Playing or SoundState.Paused))
                return;

            _state = SoundState.Stopped;

            _audioSource.Stop();
        }

        public void Pause()
        {
            if (_state != SoundState.Playing)
                return;

            _state = SoundState.Paused;

            _audioSource.Pause();
        }

        public void Unpause()
        {
            if (_state != SoundState.Paused)
                return;

            _state = SoundState.Playing;

            _audioSource.UnPause();
        }

        public void ApplySettingsFromAsset(SoundMetaAsset soundMetaAsset)
        {
            CheckIsNotReleased();
            _soundMetaAsset = soundMetaAsset;
            _audioSource.clip = soundMetaAsset.AudioClip;
            _audioSource.volume = soundMetaAsset.Volume;
            _audioSource.loop = soundMetaAsset.IsLooping;
            _audioSource.maxDistance = soundMetaAsset.MaxHearDistance;

            if (soundMetaAsset.RandomPitch)
            {
                _audioSource.pitch = UnityEngine.Random
                    .Range(1f - soundMetaAsset.RandomPitchRange, 1f + soundMetaAsset.RandomPitchRange);
            }
        }

        public void SetPosition(Vector3 position)
        {
            CheckIsNotReleased();
            _audioSource.transform.position = position;
        }

        public void SetSpatial(bool spatial)
        {
            CheckIsNotReleased();
            _audioSource.spatialBlend = spatial ? 1f : 0f;
        }

        private void CheckIsNotReleased()
        {
            if (_state == SoundState.Released)
            {
                throw new InvalidOperationException(
                    $"SoundInstance has already been released. Check state before calling a method.");
            }
        }
    }
}
