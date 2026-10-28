namespace Game.Services.LoggingService
{
    internal abstract class GameLogger : IGameLogger
    {
        protected readonly LogChannelConfig _config;

        protected GameLogger(LogChannelConfig config)
        {
            _config = config;
        }

        public void Log(string message)
        {
            if (_config.Muted)
                return;

            LogMessage(message);
        }

        public void LogWarning(string message)
        {
            if (_config.Muted)
                return;

            LogWarningMessage(message);
        }

        public void LogError(string message)
        {
            if (_config.Muted)
                return;

            LogErrorMessage(message);
        }

        protected virtual void LogMessage(string message) { }
        protected virtual void LogWarningMessage(string message) { }
        protected virtual void LogErrorMessage(string message) { }
    }
}
