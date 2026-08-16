namespace VisionFramework.Core.Configuration
{
    /// <summary>
    /// 相机配置——曝光、增益、连接信息。
    /// </summary>
    public class CameraConfig
    {
        public string CameraType { get; set; } = "HikCamera";

        /// <summary>相机 IP 或序列号。</summary>
        public string ConnectionString { get; set; } = "192.168.1.64";

        /// <summary>触发模式：Software=软件触发，Hardware=硬件触发，Continuous=连续采集。</summary>
        public string TriggerMode { get; set; } = "Software";

        /// <summary>曝光时间（微秒）。</summary>
        public double ExposureTime { get; set; } = 10000;

        /// <summary>增益（dB）。</summary>
        public double Gain { get; set; } = 0;

        /// <summary>图像宽度。</summary>
        public int Width { get; set; } = 2592;

        /// <summary>图像高度。</summary>
        public int Height { get; set; } = 1944;

        /// <summary>像素格式：Mono8 / BayerRG8 等。</summary>
        public string PixelFormat { get; set; } = "Mono8";

        /// <summary>是否水平翻转。</summary>
        public bool FlipHorizontal { get; set; } = false;

        /// <summary>是否垂直翻转。</summary>
        public bool FlipVertical { get; set; } = false;
    }
}
