using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Cognex.VisionPro;
using Cognex.VisionPro.ToolBlock;
using VisionFramework.Core.Algorithms;

namespace VisionFramework.VisionPro
{
    /// <summary>
    /// CogToolBlock 算法适配器。
    /// 从原 VisionProHostControl 提取的 ToolBlock 逻辑。
    /// </summary>
    public class ToolBlockAlgorithm : IVisionAlgorithm
    {
        public string Name => "CogToolBlock";
        public AlgorithmKind Kind => AlgorithmKind.ToolBlock;
        public bool IsInitialized { get; private set; }

        private CogToolBlock _toolBlock;
        private ICogRecord _lastRecord;

        public void Initialize(string vppPath)
        {
            _toolBlock = (CogToolBlock)CogSerializer.LoadObjectFromFile(vppPath);
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
                if (cogImage != null)
                {
                    foreach (CogToolBlockTerminal t in _toolBlock.Inputs)
                        if (typeof(ICogImage).IsAssignableFrom(t.ValueType))
                            t.Value = cogImage;
                }
                if (inputs != null)
                {
                    foreach (var kv in inputs)
                    {
                        if (_toolBlock.Inputs.Contains(kv.Key))
                            _toolBlock.Inputs[kv.Key].Value = ConvertValue(kv.Value, _toolBlock.Inputs[kv.Key].ValueType);
                    }
                }
                _toolBlock.Run();
                _lastRecord = _toolBlock.CreateLastRunRecord();
                sw.Stop();

                var outputs = new Dictionary<string, object>();
                foreach (CogToolBlockTerminal t in _toolBlock.Outputs)
                    outputs[t.Name] = t.Value;

                var result = DetectionResult.Success(outputs);
                result.DurationMs = sw.ElapsedMilliseconds;
                result.Record = _lastRecord;
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

        public List<TerminalInfo> GetInputTerminals()
        {
            var list = new List<TerminalInfo>();
            if (!IsInitialized) return list;
            foreach (CogToolBlockTerminal t in _toolBlock.Inputs)
                list.Add(new TerminalInfo
                {
                    Name = t.Name,
                    TypeName = t.ValueType?.Name,
                    Value = t.Value,
                    IsImage = typeof(ICogImage).IsAssignableFrom(t.ValueType),
                    IsOutput = false
                });
            return list;
        }

        public List<TerminalInfo> GetOutputTerminals()
        {
            var list = new List<TerminalInfo>();
            if (!IsInitialized) return list;
            foreach (CogToolBlockTerminal t in _toolBlock.Outputs)
                list.Add(new TerminalInfo
                {
                    Name = t.Name,
                    TypeName = t.ValueType?.Name,
                    Value = t.Value,
                    IsOutput = true
                });
            return list;
        }

        public object GetLastRunRecord() => _lastRecord;

        private static object ConvertValue(object value, Type targetType)
        {
            if (value == null) return null;
            if (targetType == typeof(double)) return Convert.ToDouble(value);
            if (targetType == typeof(float)) return Convert.ToSingle(value);
            if (targetType == typeof(int)) return Convert.ToInt32(value);
            if (targetType == typeof(bool)) return Convert.ToBoolean(value);
            return Convert.ChangeType(value, targetType);
        }

        public void Dispose()
        {
            _toolBlock = null;
            IsInitialized = false;
        }
    }
}