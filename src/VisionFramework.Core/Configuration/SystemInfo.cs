namespace VisionFramework.Core.Configuration
{
    /// <summary>
    /// 系统信息——版本号、设计者、VisionPro 路径。
    /// </summary>
    public static class SystemInfo
    {
        public const string Version = "1.0.0";
        public const string Designer = "姜泽凯";
        public const string BuildDate = "2026-08-16";
        public const string Description = "分层架构机器视觉框架";

        /// <summary>QuickBuild.exe 路径，可在系统信息界面修改。</summary>
        public static string VisionProPath { get; set; } = @"E:\Software\Cognex\VisionPro\bin\Cognex.VisionPro.QuickBuild.exe";
    }
}
