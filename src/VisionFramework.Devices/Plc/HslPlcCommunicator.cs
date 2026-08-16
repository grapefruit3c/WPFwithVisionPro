using System;
using VisionFramework.Core.Devices;

namespace VisionFramework.Devices.Plc
{
    /// <summary>
    /// HslCommunication PLC 通信实现（占位）。
    /// 后续集成 HslCommunication：
    ///   1. NuGet 安装 HslCommunication
    ///   2. 根据 PLC 类型创建 SiemensS7Net / MitsubishiMelsecNet / OmronCipNet 等
    ///   3. Connect → Read/Write → Disconnect
    /// </summary>
    public class HslPlcCommunicator : IPlcCommunicator
    {
        public bool IsConnected { get; private set; }
        public event EventHandler Disconnected;
        public event EventHandler<DeviceErrorEventArgs> ErrorOccurred;

        public bool Connect(string ipAddress, int port = 0)
        {
            throw new NotImplementedException("待集成 HslCommunication");
        }

        public void Disconnect() { IsConnected = false; }

        public bool ReadBool(string address) { throw new NotImplementedException(); }
        public short ReadShort(string address) { throw new NotImplementedException(); }
        public int ReadInt(string address) { throw new NotImplementedException(); }
        public float ReadFloat(string address) { throw new NotImplementedException(); }
        public string ReadString(string address, ushort length) { throw new NotImplementedException(); }

        public void Write(string address, bool value) { throw new NotImplementedException(); }
        public void Write(string address, short value) { throw new NotImplementedException(); }
        public void Write(string address, int value) { throw new NotImplementedException(); }
        public void Write(string address, float value) { throw new NotImplementedException(); }
        public void Write(string address, string value) { throw new NotImplementedException(); }

        public void Dispose() { IsConnected = false; }
    }
}