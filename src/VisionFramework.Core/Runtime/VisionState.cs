namespace VisionFramework.Core.Runtime
{
    /// <summary>
    /// 视觉系统运行状态枚举。
    /// 用于状态机管理运行流程，防止异常操作（如处理中重复触发）。
    /// </summary>
    public enum VisionState
    {
        /// <summary>待机，等待触发信号</summary>
        Idle,
        /// <summary>采集中</summary>
        Grabbing,
        /// <summary>算法处理中</summary>
        Processing,
        /// <summary>输出结果中（写 PLC、存数据）</summary>
        Outputting,
        /// <summary>异常状态，需操作员确认</summary>
        Error,
        /// <summary>已停止</summary>
        Stopped
    }

    /// <summary>状态转换事件参数。</summary>
    public class StateChangedEventArgs : System.EventArgs
    {
        public VisionState OldState { get; }
        public VisionState NewState { get; }
        public string Reason { get; }

        public StateChangedEventArgs(VisionState oldState, VisionState newState, string reason = null)
        {
            OldState = oldState;
            NewState = newState;
            Reason = reason;
        }
    }
}