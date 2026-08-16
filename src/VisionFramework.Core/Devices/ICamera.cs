using System;

namespace VisionFramework.Core.Devices
{
    /// <summary>
    /// 相机设备抽象接口。
    /// 实现类：HikCamera（海康）、FileCamera（离线文件）、未来的大华相机等。
    /// 借鉴 OpenIVS 的设备客户端抽象模式。
    /// </summary>
    public interface ICamera : IDisposable
    {
        string Name { get; }
        bool IsConnected { get; }
        event EventHandler<ImageCapturedEventArgs> ImageCaptured;
        event EventHandler<DeviceErrorEventArgs> ErrorOccurred;

        bool Connect();
        bool Connect(string connectionString);
        void Disconnect();
        void StartGrab();
        void StopGrab();
        /// <summary>软件触发一次采集（仅触发模式下有效）。</summary>
        void TriggerOnce();
    }

    public class ImageCapturedEventArgs : EventArgs
    {
        public object Image { get; }
        public DateTime Timestamp { get; }

        public ImageCapturedEventArgs(object image)
        {
            Image = image;
            Timestamp = DateTime.Now;
        }
    }

    public class DeviceErrorEventArgs : EventArgs
    {
        public string Message { get; }
        public Exception Exception { get; }

        public DeviceErrorEventArgs(string message, Exception ex = null)
        {
            Message = message;
            Exception = ex;
        }
    }
}