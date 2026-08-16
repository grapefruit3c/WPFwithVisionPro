using System;
using System.IO;
using VisionFramework.Core.Devices;

namespace VisionFramework.Devices.Cameras
{
    /// <summary>
    /// 文件虚拟相机——离线模式用，从磁盘加载图像模拟采集。
    /// 返回文件路径，由算法层负责转换为 ICogImage。
    /// </summary>
    public class FileCamera : ICamera
    {
        public string Name => "FileCamera";
        public bool IsConnected { get; private set; }
        public event EventHandler<ImageCapturedEventArgs> ImageCaptured;
        public event EventHandler<DeviceErrorEventArgs> ErrorOccurred;

        private string[] _imageFiles;
        private int _index;

        public bool Connect() { IsConnected = true; return true; }
        public bool Connect(string connectionString)
        {
            if (Directory.Exists(connectionString))
            {
                var files = new System.Collections.Generic.List<string>();
                files.AddRange(Directory.GetFiles(connectionString, "*.idb"));
                files.AddRange(Directory.GetFiles(connectionString, "*.bmp"));
                files.AddRange(Directory.GetFiles(connectionString, "*.jpg"));
                _imageFiles = files.ToArray();
            }
            else
            {
                _imageFiles = new[] { connectionString };
            }
            IsConnected = true;
            return true;
        }

        public void Disconnect() { IsConnected = false; }
        public void StartGrab() { }
        public void StopGrab() { }

        public void TriggerOnce()
        {
            if (_imageFiles == null || _imageFiles.Length == 0) return;
            var file = _imageFiles[_index % _imageFiles.Length];
            _index++;
            ImageCaptured?.Invoke(this, new ImageCapturedEventArgs(file));
        }

        public void LoadImage(string path)
        {
            ImageCaptured?.Invoke(this, new ImageCapturedEventArgs(path));
        }

        public void Dispose() { IsConnected = false; }
    }
}
