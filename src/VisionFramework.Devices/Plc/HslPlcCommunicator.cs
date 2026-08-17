using System;
using HslCommunication;
using HslCommunication.Profinet.Siemens;
using VisionFramework.Core.Devices;

namespace VisionFramework.Devices.Plc
{
    /// <summary>
    /// 基于 HslCommunication 的 PLC 通信实现。
    /// 支持西门子 S7（S7-200/300/400/1200/1500）、三菱、欧姆龙、Modbus TCP。
    /// </summary>
    public class HslPlcCommunicator : IPlcCommunicator
    {
        private object _plc;
        private string _plcType = "Siemens";

        public bool IsConnected { get; private set; }
        public event EventHandler Disconnected;
        public event EventHandler<DeviceErrorEventArgs> ErrorOccurred;

        /// <summary>当前连接的 IP。</summary>
        public string IpAddress { get; private set; }

        public bool Connect(string ipAddress, int port = 0)
        {
            try
            {
                Disconnect();

                if (port <= 0) port = 102;

                switch (_plcType)
                {
                    case "Siemens":
                        var s7 = new SiemensS7Net(SiemensPLCS.S1200, ipAddress);
                        // Rack/Slot 通过 ConnectServer 前设置
                        _plc = s7;
                        break;
                    case "Mitsubishi":
                        _plc = new HslCommunication.Profinet.Melsec.MelsecMcNet(ipAddress, port);
                        break;
                    case "Omron":
                        _plc = new HslCommunication.Profinet.Omron.OmronFinsNet(ipAddress, port);
                        break;
                    case "Modbus":
                        _plc = new HslCommunication.ModBus.ModbusTcpNet(ipAddress, port);
                        break;
                    default:
                        _plc = new SiemensS7Net(SiemensPLCS.S1200, ipAddress);
                        break;
                }

                OperateResult connectResult;
                if (_plc is SiemensS7Net s7Net)
                {
                    s7Net.Rack = 0;
                    s7Net.Slot = 1;
                    s7Net.Port = port;
                    connectResult = s7Net.ConnectServer();
                }
                else
                {
                    // 反射调用 ConnectServer()
                    var method = _plc.GetType().GetMethod("ConnectServer", Type.EmptyTypes);
                    connectResult = method?.Invoke(_plc, null) as OperateResult;
                }

                if (connectResult != null && connectResult.IsSuccess)
                {
                    IsConnected = true;
                    IpAddress = ipAddress;
                    return true;
                }

                ErrorOccurred?.Invoke(this, new DeviceErrorEventArgs(
                    connectResult?.Message ?? "连接失败", null));
                return false;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, new DeviceErrorEventArgs(
                    $"连接异常: {ex.Message}", ex));
                IsConnected = false;
                return false;
            }
        }

        /// <summary>
        /// 带 Rack/Slot 的 S7 连接。
        /// </summary>
        public bool Connect(string ipAddress, int port, byte rack, byte slot)
        {
            try
            {
                Disconnect();
                if (port <= 0) port = 102;

                var s7 = new SiemensS7Net(SiemensPLCS.S1200, ipAddress)
                {
                    Rack = rack,
                    Slot = slot,
                    Port = port
                };
                _plc = s7;
                _plcType = "Siemens";

                var result = s7.ConnectServer();
                if (result.IsSuccess)
                {
                    IsConnected = true;
                    IpAddress = ipAddress;
                    return true;
                }

                ErrorOccurred?.Invoke(this, new DeviceErrorEventArgs(result.Message, null));
                return false;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, new DeviceErrorEventArgs(
                    $"连接异常: {ex.Message}", ex));
                IsConnected = false;
                return false;
            }
        }

        public void SetPlcType(string plcType)
        {
            _plcType = plcType;
        }

        public void Disconnect()
        {
            try
            {
                if (_plc != null)
                {
                    var method = _plc.GetType().GetMethod("ConnectClose", Type.EmptyTypes);
                    method?.Invoke(_plc, null);
                    _plc = null;
                }
            }
            catch { }
            finally
            {
                if (IsConnected)
                {
                    IsConnected = false;
                    Disconnected?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        // ═══ 读取操作 ═══

        public bool ReadBool(string address)
        {
            var result = InvokeRead<OperateResult<bool>>("ReadBool", address);
            return result?.Content ?? false;
        }

        public short ReadShort(string address)
        {
            var result = InvokeRead<OperateResult<short>>("ReadInt16", address);
            return result?.Content ?? 0;
        }

        public int ReadInt(string address)
        {
            var result = InvokeRead<OperateResult<int>>("ReadInt32", address);
            return result?.Content ?? 0;
        }

        public float ReadFloat(string address)
        {
            var result = InvokeRead<OperateResult<float>>("ReadFloat", address);
            return result?.Content ?? 0f;
        }

        public string ReadString(string address, ushort length)
        {
            try
            {
                if (_plc is SiemensS7Net s7)
                    return s7.ReadString(address, length).Content ?? "";
                var method = _plc?.GetType().GetMethod("ReadString", new[] { typeof(string), typeof(ushort) });
                var r = method?.Invoke(_plc, new object[] { address, length }) as OperateResult<string>;
                return r?.Content ?? "";
            }
            catch { return ""; }
        }

        // ═══ 写入操作 ═══

        public void Write(string address, bool value)
        {
            try
            {
                if (_plc is SiemensS7Net s7)
                    s7.Write(address, value);
                else
                    _plc?.GetType().GetMethod("Write", new[] { typeof(string), typeof(bool) })
                        ?.Invoke(_plc, new object[] { address, value });
            }
            catch (Exception ex) { ErrorOccurred?.Invoke(this, new DeviceErrorEventArgs(ex.Message, ex)); }
        }

        public void Write(string address, short value)
        {
            try
            {
                if (_plc is SiemensS7Net s7)
                    s7.Write(address, value);
                else
                    _plc?.GetType().GetMethod("Write", new[] { typeof(string), typeof(short) })
                        ?.Invoke(_plc, new object[] { address, value });
            }
            catch (Exception ex) { ErrorOccurred?.Invoke(this, new DeviceErrorEventArgs(ex.Message, ex)); }
        }

        public void Write(string address, int value)
        {
            try
            {
                if (_plc is SiemensS7Net s7)
                    s7.Write(address, value);
                else
                    _plc?.GetType().GetMethod("Write", new[] { typeof(string), typeof(int) })
                        ?.Invoke(_plc, new object[] { address, value });
            }
            catch (Exception ex) { ErrorOccurred?.Invoke(this, new DeviceErrorEventArgs(ex.Message, ex)); }
        }

        public void Write(string address, float value)
        {
            try
            {
                if (_plc is SiemensS7Net s7)
                    s7.Write(address, value);
                else
                    _plc?.GetType().GetMethod("Write", new[] { typeof(string), typeof(float) })
                        ?.Invoke(_plc, new object[] { address, value });
            }
            catch (Exception ex) { ErrorOccurred?.Invoke(this, new DeviceErrorEventArgs(ex.Message, ex)); }
        }

        public void Write(string address, string value)
        {
            try
            {
                if (_plc is SiemensS7Net s7)
                    s7.Write(address, value);
                else
                    _plc?.GetType().GetMethod("Write", new[] { typeof(string), typeof(string) })
                        ?.Invoke(_plc, new object[] { address, value });
            }
            catch (Exception ex) { ErrorOccurred?.Invoke(this, new DeviceErrorEventArgs(ex.Message, ex)); }
        }

        // ═══ 辅助方法 ═══

        private T InvokeRead<T>(string methodName, string address) where T : class
        {
            try
            {
                var method = _plc?.GetType().GetMethod(methodName, new[] { typeof(string) });
                return method?.Invoke(_plc, new object[] { address }) as T;
            }
            catch { return null; }
        }

        public void Dispose()
        {
            Disconnect();
        }
    }
}
