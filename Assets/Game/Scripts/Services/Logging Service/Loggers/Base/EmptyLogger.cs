namespace Game.Services.LoggingService
{
    internal sealed class EmptyLogger : GameLogger
    {
        public static EmptyLogger Instance = Instance ??= new EmptyLogger(new LogChannelConfig());

        private EmptyLogger(LogChannelConfig config) : base(config) { }
    }
}
