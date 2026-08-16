using System;

namespace VisionFramework.Core.Devices
{
    /// <summary>
    /// PLC 通信抽象接口。
    /// 实现类：HslPlcCommunicator（HslCommunication）、ModbusPlcCommunicator（Modbus）。
    /// 借鉴 OpenIVS 的 IModbusClient 抽象模式，支持多种通信协议。
    /// </summary>
    public interface IPlcCommunicator : IDisposable
    {
        bool IsConnected { get; }
        event EventHandler Disconnected;
        event EventHandler<DeviceErrorEventArgs> ErrorOccurred;

        bool Connect(string ipAddress, int port = 0);
        void Disconnect();

        bool ReadBool(string address);
        short ReadShort(string address);
        int ReadInt(string address);
        float ReadFloat(string address);
        string ReadString(string address, ushort length);

        void Write(string address, bool value);
        void Write(string address, short value);
        void Write(string address, int value);
        void Write(string address, float value);
        void Write(string address, string value);
    }
}