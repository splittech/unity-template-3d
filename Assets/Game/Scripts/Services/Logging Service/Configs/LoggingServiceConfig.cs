using System.Collections.Generic;
using UnityEngine;

namespace Game.Services.LoggingService
{
    [CreateAssetMenu(fileName = "Logging Service Config", menuName = "Game/Configs/Logging Service Config", order = 0)]
    public sealed class LoggingServiceConfig : ScriptableObject
    {
        [DictionaryDisplay(keyLabel = "Channel", valueLabel = "Config", keyColumnFraction = 0.3f)]
        [SerializeField]
        public Dictionary<LoggingChannel, LogChannelConfig> LogChannels;
    }
}
