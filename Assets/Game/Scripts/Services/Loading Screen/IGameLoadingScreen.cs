using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Game.Services.LoadingScreen
{
    public interface IGameLoadingScreen
    {
        IProgress<float> LoadingProgress { get; }

        UniTask ShowScreenAsync(CancellationToken ct = default);
        UniTask HideScreenAsync(CancellationToken ct = default);
        void ShowScreenImmediate();
        void HideScreenImmediate();
        void SetProgressDescription(string description);
    }
}
