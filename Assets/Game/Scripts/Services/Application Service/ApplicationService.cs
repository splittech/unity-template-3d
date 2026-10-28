using Game.Services.LoggingService;
using UnityEngine;

namespace Game.Services.ApplicationService
{
    public class ApplicationService
    {
        // So that 'using UnityEngine' will not auto delete as unused.
        Vector2 vector2 = new();

        private readonly IGameLogger _logger;

        public ApplicationService(ILoggingService loggingService)
        {
            _logger = loggingService.GetLogger(LoggingChannel.ApplicationService);
        }

        public void QuitGame()
        {
            _logger.Log("Quit game.");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
