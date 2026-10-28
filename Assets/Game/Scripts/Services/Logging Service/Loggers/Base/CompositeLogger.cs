using System.Collections.Generic;

namespace Game.Services.LoggingService
{
    internal sealed class CompositeLogger : GameLogger
    {
        private readonly List<IGameLogger> _loggers;

        public CompositeLogger(LogChannelConfig config, List<IGameLogger> loggers) : base(config)
        {
            _loggers = loggers;
        }

        protected override void LogMessage(string message)
        {
            _loggers.ForEach(logger => logger.Log(message));
        }

        protected override void LogWarningMessage(string message)
        {
            _loggers.ForEach(logger => logger.LogWarning(message));
        }

        protected override void LogErrorMessage(string message)
        {
            _loggers.ForEach(logger => logger.LogError(message));
        }
    }
}
