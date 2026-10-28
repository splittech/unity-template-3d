using UnityEngine;
using UnityEngine.Audio;

namespace Game.Services.AudioService
{
    internal sealed class AudioSettings
    {
        private readonly AudioServiceConfig _config;

        public AudioSettings(AudioServiceConfig config)
        {
            _config = config;
        }

        public void Save()
        {
            PlayerPrefs.Save();
        }

        public void ApplyAllMixerGroupVolumes()
        {
            foreach (var mixerGroupConfig in _config.MixerGroupsConfigs.Values)
            {
                MixerGroupConfig.Validate(mixerGroupConfig, _config);
                mixerGroupConfig.ApplyVolume(_config.Mixer);
            }
        }

        public void SetMixerGroupVolume(AudioMixerGroup mixerGroup, float volume)
        {
            var mixerGroupSettings = MixerGroupConfig.GetFrom(mixerGroup, _config);
            mixerGroupSettings.SetVolume(_config.Mixer, volume);
        }

        public float GetMixerGroupVolume(AudioMixerGroup mixerGroup)
        {
            var mixerGroupSettings = MixerGroupConfig.GetFrom(mixerGroup, _config);
            return mixerGroupSettings.Volume;
        }
    }
}
