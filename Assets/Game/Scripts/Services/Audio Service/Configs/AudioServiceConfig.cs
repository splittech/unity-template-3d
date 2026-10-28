using System.Collections.Generic;
using Alchemy.Inspector;
using UnityEngine;
using UnityEngine.Audio;

namespace Game.Services.AudioService
{
    [CreateAssetMenu(fileName = "Audio Service Config", menuName = "Game/Configs/Audio Service Config", order = 0)]
    public sealed class AudioServiceConfig : ScriptableObject
    {
        [BoxGroup("Audio Mixer")]
        [SerializeField] private AudioMixer _mixer;

        [DictionaryDisplay(keyLabel = "Mixer Group", valueLabel = "Config", keyColumnFraction = 0.4f)]
        [SerializeField]
        private Dictionary<AudioMixerGroup, MixerGroupConfig> _mixerGroupsConfigs = new();

        [BoxGroup("Music")]
        [SerializeField] private float _musicFadeTime = 0.3f;

        public AudioMixer Mixer => _mixer;
        public Dictionary<AudioMixerGroup, MixerGroupConfig> MixerGroupsConfigs => _mixerGroupsConfigs;
        public float MusicFadeTime => _musicFadeTime;
    }
}
