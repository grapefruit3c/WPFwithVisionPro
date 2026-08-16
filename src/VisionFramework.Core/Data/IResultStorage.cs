using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VisionFramework.Core.Algorithms;

namespace VisionFramework.Core.Data
{
    /// <summary>
    /// 结果存储接口。支持本地缓存 + 远程同步（如 MES）。
    /// 借鉴岗位要求中的“分级存储架构”。
    /// </summary>
    public interface IResultStorage
    {
        Task SaveAsync(DetectionResult result, string productName = null);
        Task<List<DetectionRecord>> SearchAsync(SearchFilter filter);
        Task<Statistics> GetStatisticsAsync(DateTime from, DateTime to);
    }

    public class DetectionRecord
    {
        public int Id { get; set; }
        public DateTime Time { get; set; }
        public string ProductName { get; set; }
        public string VppName { get; set; }
        public bool IsOk { get; set; }
        public double DurationMs { get; set; }
        public string OutputsJson { get; set; }
    }

    public class SearchFilter
    {
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public string ProductName { get; set; }
        public bool? IsOk { get; set; }
        public int Limit { get; set; } = 100;
    }

    public class Statistics
    {
        public int TotalCount { get; set; }
        public int OkCount { get; set; }
        public int NgCount { get; set; }
        public double YieldRate => TotalCount > 0 ? (double)OkCount / TotalCount * 100 : 0;
        public double AvgDurationMs { get; set; }
    }
}