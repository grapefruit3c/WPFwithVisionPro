using System;
using System.Threading.Tasks;
using VisionFramework.Core.Algorithms;
using VisionFramework.Core.Devices;

namespace VisionFramework.Core.Runtime
{
    /// <summary>
    /// 视觉系统调度器接口。
    /// 借鉴 OpenIVS 的 MainLoopManager 回调式设计，
    /// 但用事件驱动替代回调，更符合 C# 惯例。
    /// </summary>
    public interface IVisionOrchestrator
    {
        VisionState CurrentState { get; }
        ICamera Camera { get; }
        IPlcCommunicator Plc { get; }
        IVisionAlgorithm Algorithm { get; }

        event EventHandler<StateChangedEventArgs> StateChanged;
        event EventHandler<DetectionResult> DetectionComplete;
        event EventHandler<Exception> ErrorOccurred;

        void Initialize(ICamera camera, IPlcCommunicator plc, IVisionAlgorithm algorithm);
        Task<DetectionResult> RunOnceAsync(object image);
        void Start();
        void Stop();
        void ResetError();
    }
}