using System.Xml.Serialization;

namespace VisionFramework.Core.Configuration
{
    /// <summary>
    /// PLC 通信配置——握手时序与结果回写地址。
    /// </summary>
    public class PlcConfig
    {
        public string IpAddress { get; set; } = "192.168.1.100";
        public int Port { get; set; } = 0;

        /// <summary>触发信号地址（PLC→视觉，PLC 写 1 触发检测）。</summary>
        public string TriggerAddress { get; set; } = "M100.0";

        /// <summary>触发应答地址（视觉→PLC，收到触发后写 1 应答）。</summary>
        public string TriggerAckAddress { get; set; } = "M100.1";

        /// <summary>结果回写地址（视觉→PLC，OK/NG 信号）。</summary>
        public string ResultAddress { get; set; } = "M200.0";

        /// <summary>结果数据地址（视觉→PLC，测量数值）。</summary>
        public string ResultDataAddress { get; set; } = "D100";

        /// <summary>心跳信号地址（视觉→PLC，定时翻转）。</summary>
        public string HeartbeatAddress { get; set; } = "M300.0";

        /// <summary>心跳周期（毫秒）。</summary>
        public int HeartbeatIntervalMs { get; set; } = 1000;

        /// <summary>Ping 超时阈值（毫秒）。</summary>
        public int PingTimeoutMs { get; set; } = 3000;

        /// <summary>PLC 类型（西门子/三菱/欧姆龙等）。</summary>
        public string PlcType { get; set; } = "Siemens";
    }
}
