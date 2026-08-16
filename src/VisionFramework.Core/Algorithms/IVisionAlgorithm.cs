using System;
using System.Collections.Generic;

namespace VisionFramework.Core.Algorithms
{
    /// <summary>
    /// 视觉算法插件接口。
    /// 借鉴 MachineVision 的 ITemplateMatchService 策略模式设计。
    /// 实现类：ToolBlockAlgorithm、QuickBuildAlgorithm、ToolGroupAlgorithm。
    /// 每种 VPP 类型一个实现，通过 AlgorithmFactory 创建。
    /// </summary>
    public interface IVisionAlgorithm : IDisposable
    {
        string Name { get; }
        AlgorithmKind Kind { get; }
        bool IsInitialized { get; }

        void Initialize(string vppPath);
        DetectionResult Detect(object image);
        DetectionResult Detect(object image, Dictionary<string, object> inputs);

        /// <summary>获取输入终端定义（用于 UI 动态生成参数面板）。</summary>
        List<TerminalInfo> GetInputTerminals();
        /// <summary>获取输出终端定义（用于 UI 展示结果）。</summary>
        List<TerminalInfo> GetOutputTerminals();
        /// <summary>获取上次运行的结果记录（用于叠加图形显示）。</summary>
        object GetLastRunRecord();
    }

    public enum AlgorithmKind
    {
        Unknown,
        ToolBlock,
        QuickBuild,
        ToolGroup,
        SingleTool
    }

    public class TerminalInfo
    {
        public string Name { get; set; }
        public string TypeName { get; set; }
        public object Value { get; set; }
        public bool IsImage { get; set; }
        public bool IsOutput { get; set; }
    }
}