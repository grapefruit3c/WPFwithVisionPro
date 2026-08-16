using System;
using System.Threading;
using System.Threading.Tasks;
using VisionFramework.Core.Algorithms;
using VisionFramework.Core.Devices;
using VisionFramework.Core.Runtime;

namespace VisionFramework.Runtime
{
    /// <summary>
    /// 视觉系统调度器——协调相机、算法、PLC。
    /// 借鉴 OpenIVS 的 MainLoopManager 回调式设计，改用事件驱动。
    /// </summary>
    public class VisionOrchestrator : IVisionOrchestrator
    {
        private readonly VisionStateMachine _stateMachine = new VisionStateMachine();
        private ImageBufferQueue<object> _imageQueue;
        private Thread _processThread;
        private CancellationTokenSource _cts;
        private bool _running;

        public VisionState CurrentState => _stateMachine.CurrentState;
        public ICamera Camera { get; private set; }
        public IPlcCommunicator Plc { get; private set; }
        public IVisionAlgorithm Algorithm { get; private set; }

        public event EventHandler<StateChangedEventArgs> StateChanged
        { add { _stateMachine.StateChanged += value; } remove { _stateMachine.StateChanged -= value; } }
        public event EventHandler<DetectionResult> DetectionComplete;
        public event EventHandler<Exception> ErrorOccurred;

        public void Initialize(ICamera camera, IPlcCommunicator plc, IVisionAlgorithm algorithm)
        {
            Camera = camera;
            Plc = plc;
            Algorithm = algorithm;
            _imageQueue = new ImageBufferQueue<object>(3);

            if (camera != null)
            {
                camera.ImageCaptured += (s, e) =>
                {
                    if (_stateMachine.CanTrigger)
                    {
                        _stateMachine.TransitionTo(VisionState.Grabbing, "相机采集完成");
                        _imageQueue.Enqueue(e.Image);
                    }
                };
                camera.ErrorOccurred += (s, e) =>
                {
                    _stateMachine.TransitionTo(VisionState.Error, e.Message);
                    ErrorOccurred?.Invoke(this, e.Exception);
                };
            }
        }

        public async Task<DetectionResult> RunOnceAsync(object image)
        {
            if (!_stateMachine.CanTrigger)
                return DetectionResult.Fail($"当前状态 {_stateMachine.CurrentState} 不允许运行");

            _stateMachine.TransitionTo(VisionState.Grabbing, "手动触发");
            _stateMachine.TransitionTo(VisionState.Processing, "开始处理");

            try
            {
                var result = await Task.Run(() => Algorithm.Detect(image));
                _stateMachine.TransitionTo(VisionState.Outputting, "处理完成");

                // 写 PLC 结果（如果连接）
                if (Plc?.IsConnected == true)
                {
                    try
                    {
                        Plc.Write("M200.0", result.IsOk);
                        if (result.Outputs.Count > 0)
                            Plc.Write("D100", (float)0);
                    }
                    catch (Exception ex)
                    {
                        ErrorOccurred?.Invoke(this, new Exception($"PLC 写入失败: {ex.Message}", ex));
                    }
                }

                _stateMachine.TransitionTo(VisionState.Idle, "完成");
                DetectionComplete?.Invoke(this, result);
                return result;
            }
            catch (Exception ex)
            {
                _stateMachine.TransitionTo(VisionState.Error, ex.Message);
                ErrorOccurred?.Invoke(this, ex);
                return DetectionResult.Fail(ex.Message, ex);
            }
        }

        public void Start()
        {
            if (_running) return;
            _running = true;
            _cts = new CancellationTokenSource();
            _processThread = new Thread(() => ProcessLoop(_cts.Token))
            {
                IsBackground = true,
                Name = "VisionProcess"
            };
            _processThread.Start();
        }

        public void Stop()
        {
            _running = false;
            _cts?.Cancel();
            _imageQueue?.CompleteAdding();
            _stateMachine.TransitionTo(VisionState.Stopped, "用户停止");
        }

        public void ResetError()
        {
            _stateMachine.ResetError();
        }

        private void ProcessLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var image = _imageQueue.Dequeue(token);
                    _stateMachine.TransitionTo(VisionState.Processing, "队列取出图像");

                    var result = Algorithm.Detect(image);

                    _stateMachine.TransitionTo(VisionState.Outputting, "处理完成");
                    if (Plc?.IsConnected == true)
                    {
                        try { Plc.Write("M200.0", result.IsOk); }
                        catch { }
                    }

                    _stateMachine.TransitionTo(VisionState.Idle, "完成");
                    DetectionComplete?.Invoke(this, result);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    _stateMachine.TransitionTo(VisionState.Error, ex.Message);
                    ErrorOccurred?.Invoke(this, ex);
                }
            }
        }

        public void Dispose()
        {
            Stop();
            _imageQueue?.Dispose();
            _cts?.Dispose();
        }
    }
}