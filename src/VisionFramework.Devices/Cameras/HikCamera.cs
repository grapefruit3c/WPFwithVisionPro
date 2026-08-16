using System;
using VisionFramework.Core.Devices;

namespace VisionFramework.Devices.Cameras
{
    /// <summary>
    /// 海康相机实现（占位）。
    /// 后续集成海康 MVS SDK：
    ///   1. 引用 MvCameraControl.Net.dll
    ///   2. 枚举设备 → 打开设备 → 注册回调 → 开始采集
    ///   3. 在回调中把 IntPtr 图像数据转为 ICogImage
    /// </summary>
    public class HikCamera : ICamera
    {
        public string Name => "HikCamera";
        public bool IsConnected { get; private set; }
        public event EventHandler<ImageCapturedEventArgs> ImageCaptured;
        public event EventHandler<DeviceErrorEventArgs> ErrorOccurred;

        public bool Connect() { throw new NotImplementedException("待集成海康 MVS SDK"); }
        public bool Connect(string connectionString) { return Connect(); }
        public void Disconnect() { IsConnected = false; }
        public void StartGrab() { throw new NotImplementedException(); }
        public void StopGrab() { }
        public void TriggerOnce() { throw new NotImplementedException(); }
        public void Dispose() { IsConnected = false; }
    }
}