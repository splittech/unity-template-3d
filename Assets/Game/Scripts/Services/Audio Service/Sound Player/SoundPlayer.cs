using System.Collections.Generic;
using Alchemy.Inspector;
using Game.Services.LoggingService;
using UnityEngine;
using UnityEngine.Audio;
using VContainer;

namespace Game.Services.AudioService
{
    public class SoundPlayer : MonoBehaviour
    {
        [BoxGroup("Sound Pools")]
        [DictionaryDisplay(keyLabel = "Mixer Group", valueLabel = "Pool Size", keyColumnFraction = 0.4f)]
        [SerializeField] private Dictionary<AudioMixerGroup, int> _soundPoolConfigs;

        private readonly Dictionary<AudioMixerGroup, SoundPool> _soundPools = new();

        private IGameLogger _logger;

        [Inject]
        public void Construct(ILoggingService loggingService)
        {
            _logger = loggingService.GetLogger(LoggingChannel.AudioService);

            CreatePools();
        }

        private void CreatePools()
        {
            foreach (var soundPoolConfig in _soundPoolConfigs)
            {
                AudioMixerGroup mixerGroup = soundPoolConfig.Key;
                int size = soundPoolConfig.Value;

                SoundPool soundPool = CreateSoundPool(mixerGroup, size);
                _soundPools.Add(mixerGroup, soundPool);
            }
        }

        public SoundInstance PlaySound(SoundMetaAsset soundAsset, bool autoStart)
        {
            if (!SetupSound(soundAsset, out var sound))
                return null;

            if (autoStart)
                sound.Play();

            return sound;
        }

        public SoundInstance PlaySpatialSound(SoundMetaAsset soundAsset, Vector3 position, bool autoStart)
        {
            if (!SetupSound(soundAsset, out var sound))
                return null;

            sound.SetSpatial(true);
            sound.SetPosition(position);
            if (autoStart)
                sound.Play();

            return sound;
        }

        private SoundPool CreateSoundPool(AudioMixerGroup mixerGroup, int size)
        {
            GameObject poolObject = new($"{mixerGroup.name} (pool)", typeof(SoundPool));
            poolObject.transform.SetParent(transform);

            SoundPool soundPool = poolObject.GetComponent<SoundPool>();
            soundPool.AudioMixerGroup = mixerGroup;
            soundPool.Initialize(size);

            return soundPool;
        }

        private bool SetupSound(SoundMetaAsset soundAsset, out SoundInstance soundInstance)
        {
            soundInstance = null;

            if (!_soundPools.TryGetValue(soundAsset.MixerGroup, out var soundPool))
            {
                _logger.LogError($"There is no sound pool with mixer group '{soundAsset.MixerGroup}' of sound asset '{soundAsset.name}'.");
                return false;
            }

            soundInstance = soundPool.Get(soundAsset);

            if (soundInstance == null)
            {
                _logger.LogWarning($"Sound pool with mixer group '{soundAsset.MixerGroup}' is full, sound asset '{soundAsset.name}' was skipped.");
                return false;
            }

            soundInstance.ApplySettingsFromAsset(soundAsset);

            return true;
        }
    }
}
