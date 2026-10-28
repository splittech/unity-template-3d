using System;
using UnityEngine;

namespace Game.Services.LoggingService
{
    [Flags]
    public enum LoggerType
    {
        UnityLogger = 1 << 0,
        FileLogger = 1 << 1,
        ServerLogger = 1 << 2,
    }

    [Serializable]
    public sealed class LogChannelConfig
    {
        [SerializeField] private bool _muted;
        [SerializeField] private string _channelPrefix = "";
        [SerializeField] private Color _prefixColor = Color.white;
        [SerializeField] private LoggerType _loggerTypes;

        public bool Muted => _muted;
        public string ChannelPrefix => _channelPrefix;
        public Color PrefixColor => _prefixColor;
        public LoggerType LoggerTypes => _loggerTypes;
    }
}
