using System;
using Alchemy.Inspector;
using UnityEngine;
using UnityEngine.Audio;

namespace Game.Services.AudioService
{
    [CreateAssetMenu(fileName = "Sound Meta Asset", menuName = "Game/Meta Assets/Sound Meta Asset", order = 0)]
    public sealed class SoundMetaAsset : ScriptableObject
    {
        [BoxGroup("General")]
        [SerializeField] private AudioClip _audioClip;

        [BoxGroup("General")]
        [SerializeField] private AudioMixerGroup _mixerGroup;

        [BoxGroup("General")]
        [Range(0f, 1f), SerializeField] private float _volume = 1f;

        [BoxGroup("General")]
        [SerializeField] private bool _isLooping;

        [BoxGroup("General")]
        [SerializeField] private bool _randomPitch;

        [BoxGroup("General")]
        [ShowIf("_randomPitch")]
        [Range(0f, 0.5f), SerializeField] private float _randomPitchRange = 0.1f;

        [BoxGroup("Pool")]
        [SerializeField] private int _priority;

        [BoxGroup("Pool")]
        [Min(0), SerializeField] private int _maxInstances;

        [BoxGroup("Spatial")]
        [SerializeField] private float _maxHearDistance = 500f;

        public AudioClip AudioClip => _audioClip;
        public AudioMixerGroup MixerGroup => _mixerGroup;
        public float Volume => _volume;
        public bool IsLooping => _isLooping;
        public bool RandomPitch => _randomPitch;
        public float RandomPitchRange => _randomPitchRange;
        public int Priority => _priority;
        public int MaxInstances => _maxInstances;
        public float MaxHearDistance => _maxHearDistance;
    }
}
