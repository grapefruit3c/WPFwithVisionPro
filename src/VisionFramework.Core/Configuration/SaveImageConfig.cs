namespace VisionFramework.Core.Configuration
{
    /// <summary>
    /// 存图配置——格式、路径、命名规则。
    /// </summary>
    public class SaveImageConfig
    {
        /// <summary>存图格式：BMP / JPG / PNG / IDB。</summary>
        public string ImageFormat { get; set; } = "BMP";

        /// <summary>存图根目录。</summary>
        public string SavePath { get; set; } = @"D:\VisionImages";

        /// <summary>是否保存渲染图（带叠加图形的结果图）。</summary>
        public bool SaveRenderedImage { get; set; } = true;

        /// <summary>是否保存原始图。</summary>
        public bool SaveOriginalImage { get; set; } = true;

        /// <summary>文件名命名模式：{ProductId}_{Timestamp}_{Result}。</summary>
        public string NamingPattern { get; set; } = "{ProductId}_{Timestamp}_{Result}";

        /// <summary>是否在文件名中添加时间戳。</summary>
        public bool AddTimestamp { get; set; } = true;

        /// <summary>是否在文件名中添加产品 ID。</summary>
        public bool AddProductId { get; set; } = true;

        /// <summary>是否在文件名中添加检测结果（OK/NG）。</summary>
        public bool AddResult { get; set; } = true;

        /// <summary>时间戳格式。</summary>
        public string TimestampFormat { get; set; } = "yyyyMMdd_HHmmss_fff";

        /// <summary>按日期建子目录。</summary>
        public bool CreateDateFolder { get; set; } = true;

        /// <summary>按结果建子目录（OK/NG 分开存）。</summary>
        public bool CreateResultFolder { get; set; } = false;

        /// <summary>最大磁盘使用率（%），超过则停止存图并报警。</summary>
        public int MaxDiskUsagePercent { get; set; } = 90;

        /// <summary>JPG 质量（1-100），仅 ImageFormat=JPG 时有效。</summary>
        public int JpgQuality { get; set; } = 95;
    }
}
