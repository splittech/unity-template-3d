using System;
using UnityEngine;
using UnityEngine.Audio;

namespace Game.Services.AudioService
{
    [Serializable]
    public sealed class MixerGroupConfig
    {
        [SerializeField] private string _volumeExposedName;
        [SerializeField, Range(0f, 1f)] private float _volume = 0.5f;

        public float Volume => PlayerPrefs.GetFloat(VolumeKey, _volume);

        private string VolumeKey => $"{nameof(MixerGroupConfig)}.{_volumeExposedName}";

        public void SetVolume(AudioMixer mixer, float volume)
        {
            PlayerPrefs.SetFloat(VolumeKey, Mathf.Clamp01(volume));
            ApplyVolume(mixer);
        }

        public void ApplyVolume(AudioMixer mixer)
        {
            float volumeInDb = Mathf.Log10(Mathf.Clamp(Volume, 0.0001f, 1f)) * 20f;
            bool success = mixer.SetFloat(_volumeExposedName, volumeInDb);

            if (!success)
            {
                throw new InvalidOperationException(
                    "Setting mixer group volume was not successful. There may be invalid exposed name in AudioServiceConfig.");
            }
        }

        public static void Validate(MixerGroupConfig mixerGroupSettings, AudioServiceConfig config)
        {
            if (mixerGroupSettings == null)
            {
                throw new InvalidOperationException(
                    $"AudioServiceConfig '{config.name}': MixerGroupSettings is null.");
            }

            if (string.IsNullOrEmpty(mixerGroupSettings._volumeExposedName))
            {
                throw new ArgumentException(
                    $"AudioServiceConfig '{config.name}': MixerGroup volume exposed name was null or empty.");
            }
        }

        public static MixerGroupConfig GetFrom(AudioMixerGroup mixerGroup, AudioServiceConfig config)
        {
            if (mixerGroup == null)
                throw new ArgumentNullException("MixerGroup cannot be null.");

            if (!config.MixerGroupsConfigs.TryGetValue(mixerGroup, out var mixerGroupSettings))
                throw new ArgumentException($"AudioServiceConfig '{config.name}': MixerGroup is not registered.");

            Validate(mixerGroupSettings, config);

            return mixerGroupSettings;
        }
    }
}
