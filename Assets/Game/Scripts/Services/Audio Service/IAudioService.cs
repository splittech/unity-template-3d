using UnityEngine;
using UnityEngine.Audio;

namespace Game.Services.AudioService
{
    public interface IAudioService
    {
        SoundInstance PlaySound(SoundMetaAsset soundMetaAsset, bool autoStart = true);
        SoundInstance PlaySpatialSound(SoundMetaAsset soundMetaAsset, Vector3 position, bool autoStart = true);
        void ApplyAllMixerGroupVolumes();
        float GetMixerGroupVolume(AudioMixerGroup mixerGroup);
        void PlayMusic(MusicMetaAsset musicMetaAsset);
        void SetMixerGroupVolume(AudioMixerGroup mixerGroup, float volume);
        void StopMusic();
    }
}
