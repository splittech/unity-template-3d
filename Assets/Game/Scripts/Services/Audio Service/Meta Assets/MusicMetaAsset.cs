using Alchemy.Inspector;
using UnityEngine;

namespace Game.Services.AudioService
{
    [CreateAssetMenu(fileName = "Music Meta Asset", menuName = "Game/Meta Assets/Music Meta Asset", order = 0)]
    public sealed class MusicMetaAsset : ScriptableObject
    {
        [BoxGroup("General")]
        [SerializeField] private AudioClip _audioClip;

        [BoxGroup("General")]
        [SerializeField, Range(0f, 1f)] private float _volume = 1f;

        public AudioClip AudioClip => _audioClip;
        public float Volume => _volume;
    }
}
