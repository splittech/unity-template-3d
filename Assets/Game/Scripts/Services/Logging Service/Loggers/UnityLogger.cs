using UnityEngine;

namespace Game.Services.LoggingService
{
    internal sealed class UnityLogger : GameLogger
    {
        private string _prefixFormatString;

        public UnityLogger(LogChannelConfig config) : base(config)
        {
            _prefixFormatString = CreatePrefixFormatString();
        }

        protected override void LogMessage(string message)
        {
            Debug.Log($"{_prefixFormatString} {message}");
        }

        protected override void LogWarningMessage(string message)
        {
            Debug.LogWarning($"{_prefixFormatString} {message}");
        }

        protected override void LogErrorMessage(string message)
        {
            Debug.LogError($"{_prefixFormatString} {message}");
        }

        private string CreatePrefixFormatString()
        {
            string prefix = _config.ChannelPrefix;
            string color = ColorUtility.ToHtmlStringRGB(_config.PrefixColor);

            return $"<color=#{color}><b>[{prefix}]</b></color>";
        }
    }
}
