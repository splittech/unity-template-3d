using System.Collections.Generic;

namespace Game.Services.LoggingService
{
    public sealed class LoggingService : ILoggingService
    {
        private readonly LoggingServiceConfig _config;

        private readonly Dictionary<LoggingChannel, IGameLogger> _loggers = new();

        public LoggingService(LoggingServiceConfig config)
        {
            _config = config;

            CreateLoggers();
        }

        public IGameLogger GetLogger(LoggingChannel channel)
        {
            if (!_loggers.TryGetValue(channel, out var logger))
                return EmptyLogger.Instance;

            return logger;
        }

        private void CreateLoggers()
        {
            foreach (var channel in _config.LogChannels)
            {
                LoggingChannel loggingChannel = channel.Key;
                LogChannelConfig channelConfig = channel.Value;

                _loggers.Add(loggingChannel, CreateLogger(channelConfig));
            }
        }

        private IGameLogger CreateLogger(LogChannelConfig channelConfig)
        {
            List<IGameLogger> loggers = new();

            if (channelConfig.LoggerTypes.HasFlag(LoggerType.UnityLogger))
                loggers.Add(new UnityLogger(channelConfig));

            if (channelConfig.LoggerTypes.HasFlag(LoggerType.FileLogger))
                loggers.Add(new FileLogger(channelConfig));

            if (channelConfig.LoggerTypes.HasFlag(LoggerType.ServerLogger))
                loggers.Add(new ServerLogger(channelConfig));

            if (loggers.Count == 0)
                return EmptyLogger.Instance;

            if (loggers.Count == 1)
                return loggers[0];

            return new CompositeLogger(channelConfig, loggers);
        }
    }
}