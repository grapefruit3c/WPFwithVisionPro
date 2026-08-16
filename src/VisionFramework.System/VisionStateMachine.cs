using System;
using VisionFramework.Core.Runtime;

namespace VisionFramework.Runtime
{
    /// <summary>
    /// 视觉系统状态机。
    /// 管理运行流程：Idle → Grabbing → Processing → Outputting → Idle。
    /// 防止异常操作（如处理中重复触发）。
    /// </summary>
    public class VisionStateMachine
    {
        private readonly object _lock = new object();
        public VisionState CurrentState { get; private set; } = VisionState.Idle;
        public event EventHandler<StateChangedEventArgs> StateChanged;

        public bool TransitionTo(VisionState newState, string reason = null)
        {
            lock (_lock)
            {
                if (!IsValidTransition(CurrentState, newState))
                    return false;
                var old = CurrentState;
                CurrentState = newState;
                StateChanged?.Invoke(this, new StateChangedEventArgs(old, newState, reason));
                return true;
            }
        }

        private static bool IsValidTransition(VisionState from, VisionState to)
        {
            // Error 状态需要 ResetError 才能恢复
            if (from == VisionState.Error && to != VisionState.Idle)
                return false;
            // Stopped 状态需要 Start 才能恢复
            if (from == VisionState.Stopped && to != VisionState.Idle)
                return false;
            // 任何状态都可以转到 Error 或 Stopped
            if (to == VisionState.Error || to == VisionState.Stopped)
                return true;
            // Idle 可以转到 Grabbing
            if (from == VisionState.Idle && to == VisionState.Grabbing)
                return true;
            // Grabbing 可以转到 Processing
            if (from == VisionState.Grabbing && to == VisionState.Processing)
                return true;
            // Processing 可以转到 Outputting
            if (from == VisionState.Processing && to == VisionState.Outputting)
                return true;
            // Outputting 回到 Idle
            if (from == VisionState.Outputting && to == VisionState.Idle)
                return true;
            // 同状态不转
            if (from == to) return false;
            return false;
        }

        public bool CanTrigger => CurrentState == VisionState.Idle;
        public void ResetError() { TransitionTo(VisionState.Idle, "操作员确认恢复"); }
    }
}