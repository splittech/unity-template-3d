using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;

namespace Game.Services.AudioService
{
    public class SoundPool : MonoBehaviour
    {
        private AudioMixerGroup _audioMixerGroup;

        private readonly List<AudioSource> _audioSources = new();
        private readonly Dictionary<AudioSource, SoundInstance> _sounds = new();
        private readonly LinkedList<SoundInstance> _soundQueue = new();

        public AudioMixerGroup AudioMixerGroup { set => _audioMixerGroup = value; }

        private void Update()
        {
            foreach (var source in _audioSources)
            {
                SoundInstance sound = _sounds[source];

                if (sound == null)
                    continue;

                // Sound was stopped or finished playing.
                if (sound.State == SoundState.Stopped || sound.State == SoundState.Playing && !source.isPlaying)
                    sound.Release();
            }
        }

        public void Initialize(int number)
        {
            for (int i = 0; i < number; i++)
            {
                GameObject audioSourceObject = new($"{name}'s Audio Source ({i + 1})");
                audioSourceObject.transform.SetParent(transform);
                audioSourceObject.SetActive(false);

                AudioSource audioSource = audioSourceObject.AddComponent<AudioSource>();
                audioSource.outputAudioMixerGroup = _audioMixerGroup;
                audioSource.playOnAwake = false;

                _audioSources.Add(audioSource);
            }

            ValidatePoolSize();

            foreach (var source in _audioSources)
                _sounds[source] = null;
        }

        public SoundInstance Get(SoundMetaAsset soundMetaAsset)
        {
            SoundInstance soundToSteal = FindOldestInstanceOverLimit(soundMetaAsset);

            if (soundToSteal == null)
            {
                AudioSource freeAudioSource = _audioSources
                    .FirstOrDefault(source => _sounds[source] == null);

                if (freeAudioSource != null)
                    return CreateSound(freeAudioSource);

                soundToSteal = FindSoundToSteal(soundMetaAsset);

                if (soundToSteal == null)
                    return null;
            }

            AudioSource stolenAudioSource = soundToSteal.AudioSource;
            soundToSteal.Release();

            return CreateSound(stolenAudioSource);
        }

        public void Release(SoundInstance sound)
        {
            AudioSource source = sound.AudioSource;

            _soundQueue.Remove(sound);
            _sounds[source] = null;
        }

        public void ValidatePoolSize()
        {
            if (_audioSources == null)
                throw new InvalidOperationException($"SoundPool '{name}': Audio Sources list is null.");

            if (_audioSources.Count == 0)
                throw new InvalidOperationException($"SoundPool '{name}': Audio Sources list is empty.");

            foreach (var audioSource in _audioSources)
            {
                if (audioSource == null)
                    throw new InvalidOperationException($"SoundPool '{name}': Audio Sources list contains null reference.");
            }

            if (_audioSources.Distinct().Count() != _audioSources.Count)
                throw new InvalidOperationException($"SoundPool '{name}': Audio Sources list contains duplicates.");
        }

        private SoundInstance FindOldestInstanceOverLimit(SoundMetaAsset soundMetaAsset)
        {
            if (soundMetaAsset.MaxInstances <= 0)
                return null;

            SoundInstance oldestInstance = null;
            int instanceCount = 0;

            foreach (var sound in _soundQueue)
            {
                if (sound.SoundMetaAsset != soundMetaAsset)
                    continue;

                oldestInstance ??= sound;
                instanceCount++;
            }

            return instanceCount >= soundMetaAsset.MaxInstances ? oldestInstance : null;
        }

        // Steals the oldest non-looping sound with the lowest priority not higher than the new one.
        private SoundInstance FindSoundToSteal(SoundMetaAsset soundMetaAsset)
        {
            SoundInstance soundToSteal = null;

            foreach (var sound in _soundQueue)
            {
                if (sound.SoundMetaAsset.IsLooping || sound.SoundMetaAsset.Priority > soundMetaAsset.Priority)
                    continue;

                if (soundToSteal == null || sound.SoundMetaAsset.Priority < soundToSteal.SoundMetaAsset.Priority)
                    soundToSteal = sound;
            }

            return soundToSteal;
        }

        private SoundInstance CreateSound(AudioSource source)
        {
            SoundInstance sound = new(source, this);

            _soundQueue.AddLast(sound);
            _sounds[source] = sound;

            return sound;
        }
    }
}
