using Game.Services.LoggingService;
using UnityEngine;
using UnityEngine.Audio;

namespace Game.Services.AudioService
{
    public sealed class AudioService : IAudioService
    {
        private readonly IGameLogger _logger;
        private readonly MusicPlayer _musicPlayer;
        private readonly SoundPlayer _soundPlayer;
        private readonly AudioSettings _audioSettings;

        public AudioService(
            ILoggingService loggingService,
            MusicPlayer musicPlayer,
            SoundPlayer soundPlayer,
            AudioServiceConfig config)
        {
            _musicPlayer = musicPlayer;
            _soundPlayer = soundPlayer;

            _logger = loggingService.GetLogger(LoggingChannel.AudioService);

            _audioSettings = new(config);
        }

        public void PlayMusic(MusicMetaAsset musicMetaAsset)
        {
            _logger.Log($"Play music with MusicMetaAsset '{musicMetaAsset.name}'.");

            _musicPlayer.PlayMusic(musicMetaAsset);
        }

        public void StopMusic()
        {
            _logger.Log($"Stop music.");

            _musicPlayer.StopMusic();
        }

        public SoundInstance PlaySound(SoundMetaAsset soundMetaAsset, bool autoStart = true)
        {
            _logger.Log($"Play sound with SoundMetaAsset '{soundMetaAsset.name}'.");

            return _soundPlayer.PlaySound(soundMetaAsset, autoStart);
        }

        public SoundInstance PlaySpatialSound(SoundMetaAsset soundMetaAsset, Vector3 position, bool autoStart = true)
        {
            _logger.Log($"Play spatial sound with SoundMetaAsset '{soundMetaAsset.name}'.");

            return _soundPlayer.PlaySpatialSound(soundMetaAsset, position, autoStart);
        }

        public void ApplyAllMixerGroupVolumes()
        {
            _logger.Log("Apply all mixer group volumes.");

            _audioSettings.ApplyAllMixerGroupVolumes();
        }

        public void SetMixerGroupVolume(AudioMixerGroup mixerGroup, float volume)
        {
            _logger.Log($"Set volume of AudioMixerGroup '{mixerGroup.name}'.");

            _audioSettings.SetMixerGroupVolume(mixerGroup, volume);
        }

        public float GetMixerGroupVolume(AudioMixerGroup mixerGroup)
        {
            _logger.Log($"Get volume of AudioMixerGroup '{mixerGroup.name}'.");

            return _audioSettings.GetMixerGroupVolume(mixerGroup);
        }
    }
}
