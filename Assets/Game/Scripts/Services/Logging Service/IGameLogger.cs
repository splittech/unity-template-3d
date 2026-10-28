namespace Game.Services.LoggingService
{
    public interface IGameLogger
    {
        void Log(string message);
        void LogWarning(string message);
        void LogError(string message);
    }
}
