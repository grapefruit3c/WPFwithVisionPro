using System;
using Cognex.VisionPro;
using Cognex.VisionPro.ToolBlock;
using Cognex.VisionPro.ToolGroup;
using Cognex.VisionPro.QuickBuild;
using VisionFramework.Core.Algorithms;

namespace VisionFramework.VisionPro
{
    /// <summary>
    /// 算法工厂——根据 VPP 文件类型创建对应的算法实例。
    /// 借鉴 MachineVision 的策略容器 + 命名注册模式。
    /// </summary>
    public static class AlgorithmFactory
    {
        public static IVisionAlgorithm Create(string vppPath)
        {
            if (string.IsNullOrEmpty(vppPath))
                throw new ArgumentNullException(nameof(vppPath));

            object vpp = CogSerializer.LoadObjectFromFile(vppPath);
            IVisionAlgorithm algo;

            if (vpp is CogToolBlock)
                algo = new ToolBlockAlgorithm();
            else if (vpp is CogJobManager)
                algo = new QuickBuildAlgorithm();
            else
                throw new NotSupportedException($"不支持的 VPP 类型: {vpp?.GetType().Name}");

            algo.Initialize(vppPath);
            return algo;
        }

        public static AlgorithmKind DetectKind(string vppPath)
        {
            object vpp = CogSerializer.LoadObjectFromFile(vppPath);
            if (vpp is CogToolBlock) return AlgorithmKind.ToolBlock;
            if (vpp is CogJobManager) return AlgorithmKind.QuickBuild;
            if (vpp is CogToolGroup) return AlgorithmKind.ToolGroup;
            if (vpp is ICogTool) return AlgorithmKind.SingleTool;
            return AlgorithmKind.Unknown;
        }
    }
}