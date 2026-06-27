using AIInGames.Planning.Runtime;
using UnityEngine;

namespace AIInGames.Planning.Demo
{
    /// <summary>
    /// Stand-in action handler for the demo: every action "runs" for a fixed number of seconds and
    /// then reports complete. A real game would move the agent, play an animation, or drive
    /// navigation here, and report completion or failure from that work instead.
    /// </summary>
    public sealed class SimulatedActionExecutor : IActionExecutor
    {
        private readonly float m_Duration;
        private float m_FinishTime;
        private bool m_Running;

        public SimulatedActionExecutor(float duration)
        {
            m_Duration = Mathf.Max(0f, duration);
        }

        public bool CanExecute(GroundedAction action) => true;

        public void StartExecution(GroundedAction action)
        {
            m_FinishTime = Time.time + m_Duration;
            m_Running = true;
        }

        public bool IsComplete()
        {
            if (!m_Running || Time.time < m_FinishTime)
                return false;

            m_Running = false;
            return true;
        }

        public bool HasFailed() => false;

        public void Cancel() => m_Running = false;
    }
}
