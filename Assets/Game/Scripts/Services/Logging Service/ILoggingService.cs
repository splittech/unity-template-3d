namespace Game.Services.LoggingService
{
    public interface ILoggingService
    {
        public IGameLogger GetLogger(LoggingChannel channel);
    }
}