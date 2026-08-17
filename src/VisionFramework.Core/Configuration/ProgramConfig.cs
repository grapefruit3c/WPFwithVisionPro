using System.Collections.Generic;

namespace VisionFramework.Core.Configuration
{
    public class ProgramEntry
    {
        public int Number { get; set; }
        public string Name { get; set; } = "";
        public string VppPath { get; set; } = "";
    }

    /// <summary>
    /// 程序配置——PLC 程序号与 VPP 路径的映射。
    /// PLC 发送 1 运行程序 1，发送 2 运行程序 2，未发送（0）默认程序 1。
    /// </summary>
    public class ProgramConfig
    {
        public List<ProgramEntry> Programs { get; set; } = new List<ProgramEntry>();

        /// <summary>根据程序号获取 VPP 路径。programNumber <= 0 时返回第一个程序（默认）。</summary>
        public string GetVppPath(int programNumber)
        {
            if (programNumber <= 0)
                return Programs.Count > 0 ? Programs[0].VppPath : null;

            var entry = Programs.Find(p => p.Number == programNumber);
            if (entry != null)
                return entry.VppPath;

            return Programs.Count > 0 ? Programs[0].VppPath : null;
        }
    }
}
