using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Cognex.VisionPro;
using Cognex.VisionPro.ToolBlock;
using Cognex.VisionPro.ToolGroup;
using Cognex.VisionPro.QuickBuild;
using VisionFramework.Core.Algorithms;

namespace VisionFramework.VisionPro
{
    /// <summary>
    /// QuickBuild (CogJobManager) 算法适配器。
    /// 从原 VisionProHostControl 提取的 QuickBuild 逻辑，
    /// 包含多策略图像注入（终端/脚本终端/子工具遍历/反射）。
    /// </summary>
    public class QuickBuildAlgorithm : IVisionAlgorithm
    {
        public string Name => "QuickBuild";
        public AlgorithmKind Kind => AlgorithmKind.QuickBuild;
        public bool IsInitialized { get; private set; }

        private CogJobManager _jobManager;
        private int _selectedJobIndex;
        private ICogRecord _lastRecord;

        public int JobCount => _jobManager?.JobCount ?? 0;

        public void SelectJob(int index)
        {
            _selectedJobIndex = Math.Max(0, Math.Min(index, JobCount - 1));
        }

        public void Initialize(string vppPath)
        {
            _jobManager = (CogJobManager)CogSerializer.LoadObjectFromFile(vppPath);
            _selectedJobIndex = 0;
            IsInitialized = true;
        }

        public DetectionResult Detect(object image)
        {
            return Detect(image, null);
        }

        public DetectionResult Detect(object image, Dictionary<string, object> inputs)
        {
            if (!IsInitialized) return DetectionResult.Fail("算法未初始化");
            var sw = Stopwatch.StartNew();
            try
            {
                var cogImage = image as ICogImage;
                CogJob job = _jobManager.Job(_selectedJobIndex);
                ICogTool vt = job?.VisionTool;
                if (vt == null) return DetectionResult.Fail("该 Job 没有视觉工具");

                ICogRecord record;
                if (cogImage != null)
                {
                    // 有手动图片：绕过 AcqFifo，直接喂图给视觉工具
                    InjectImage(vt, cogImage);
                    vt.Run();
                    record = vt.CreateLastRunRecord() ?? job.OwnedIndependent.RealTimeResult();
                }
                else
                {
                    // 无手动图片：走 Job 正常流程
                    job.Run();
                    record = job.OwnedIndependent.RealTimeResult();
                }
                _lastRecord = record;
                sw.Stop();

                // 枚举输出（仅 CogToolBlock 类型可枚举）
                var outputs = new Dictionary<string, object>();
                if (vt is CogToolBlock tb)
                    foreach (CogToolBlockTerminal t in tb.Outputs)
                        outputs[t.Name] = t.Value;

                var result = DetectionResult.Success(outputs);
                result.DurationMs = sw.ElapsedMilliseconds;
                result.Record = record;
                result.Image = cogImage;
                return result;
            }
            catch (Exception ex)
            {
                sw.Stop();
                return new DetectionResult
                {
                    IsOk = false,
                    Message = ex.Message,
                    Exception = ex,
                    DurationMs = sw.ElapsedMilliseconds
                };
            }
        }

        /// <summary>多策略图像注入：终端→脚本终端→子工具→反射。</summary>
        private void InjectImage(ICogTool vt, ICogImage img)
        {
            if (vt is CogToolBlock tb)
            {
                bool ok = false;
                foreach (CogToolBlockTerminal t in tb.Inputs)
                    if (typeof(ICogImage).IsAssignableFrom(t.ValueType))
                    { t.Value = img; ok = true; }
                if (!ok) TrySetInputImage(tb, img);
            }
            else if (vt is CogToolGroup tg)
            {
                // 策略1：脚本终端
                string[] keys = { "InputImage", "Image", "inputImage", "image" };
                bool ok = false;
                foreach (string key in keys)
                {
                    try
                    {
                        var method = tg.GetType().GetMethod("SetScriptTerminalData", new[] { typeof(string), typeof(object) });
                        if (method != null && (bool)method.Invoke(tg, new object[] { key, img }))
                        { ok = true; break; }
                    }
                    catch { }
                }
                // 策略2：遍历子工具
                if (!ok)
                {
                    foreach (var subTool in tg.Tools)
                    {
                        if (subTool is CogToolBlock subTb)
                            foreach (CogToolBlockTerminal t in subTb.Inputs)
                                if (typeof(ICogImage).IsAssignableFrom(t.ValueType))
                                { t.Value = img; ok = true; }
                        else if (TrySetInputImage(subTool, img))
                            ok = true;
                    }
                }
            }
            else
            {
                TrySetInputImage(vt, img);
            }
        }

        private static bool TrySetInputImage(object tool, ICogImage img)
        {
            try
            {
                var prop = tool.GetType().GetProperty("InputImage");
                if (prop != null && prop.CanWrite)
                { prop.SetValue(tool, img); return true; }
            }
            catch { }
            return false;
        }

        public List<TerminalInfo> GetInputTerminals()
        {
            var list = new List<TerminalInfo>();
            if (!IsInitialized) return list;
            for (int i = 0; i < _jobManager.JobCount; i++)
            {
                CogJob job = _jobManager.Job(i);
                list.Add(new TerminalInfo
                {
                    Name = $"Job {i}: {job?.Name ?? i.ToString()}",
                    TypeName = job?.VisionTool?.GetType().Name,
                    IsOutput = false
                });
            }
            return list;
        }

        public List<TerminalInfo> GetOutputTerminals()
        {
            var list = new List<TerminalInfo>();
            if (!IsInitialized) return list;
            CogJob job = _jobManager.Job(_selectedJobIndex);
            if (job?.VisionTool is CogToolBlock tb)
                foreach (CogToolBlockTerminal t in tb.Outputs)
                    list.Add(new TerminalInfo { Name = t.Name, TypeName = t.ValueType?.Name, Value = t.Value, IsOutput = true });
            return list;
        }

        public object GetLastRunRecord() => _lastRecord;

        public void Dispose()
        {
            _jobManager = null;
            IsInitialized = false;
        }
    }
}