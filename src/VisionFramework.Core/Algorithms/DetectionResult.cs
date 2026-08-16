using System;
using System.Collections.Generic;

namespace VisionFramework.Core.Algorithms
{
    /// <summary>
    /// 检测结果统一模型。
    /// 借鉴 MachineVision 的 MatchResult 统一结果设计。
    /// </summary>
    public class DetectionResult
    {
        public bool IsOk { get; set; }
        public string Message { get; set; }
        public double DurationMs { get; set; }
        public DateTime Timestamp { get; } = DateTime.Now;

        /// <summary>输出终端的值（键=终端名，值=终端值）。</summary>
        public Dictionary<string, object> Outputs { get; } = new Dictionary<string, object>();

        /// <summary>结果记录对象（如 ICogRecord），用于 UI 叠加显示。</summary>
        public object Record { get; set; }

        /// <summary>运行时图像（用于显示区背景）。</summary>
        public object Image { get; set; }

        public Exception Exception { get; set; }

        public bool HasException => Exception != null;

        public static DetectionResult Success(Dictionary<string, object> outputs = null)
        {
            var r = new DetectionResult { IsOk = true, Message = "OK" };
            if (outputs != null)
                foreach (var kv in outputs)
                    r.Outputs[kv.Key] = kv.Value;
            return r;
        }

        public static DetectionResult Fail(string message, Exception ex = null)
        {
            return new DetectionResult { IsOk = false, Message = message, Exception = ex };
        }
    }
}