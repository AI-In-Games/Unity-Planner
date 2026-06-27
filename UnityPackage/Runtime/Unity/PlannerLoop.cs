using System;
using AIInGames.Planning.Unity;

namespace AIInGames.Planning.Runtime
{
    /// <summary>
    /// Connects a <see cref="PlannerService"/> to Unity's player loop: it installs the planning
    /// sync point (if needed) and pumps exactly one queued find each frame, in the given phase
    /// (PreUpdate by default, so results are ready before MonoBehaviour.Update). Dispose to detach.
    /// </summary>
    public sealed class PlannerLoop : IDisposable
    {
        private readonly PlannerService m_Service;
        private bool m_Attached;

        public PlannerLoop(PlannerService service, PlanningPlayerLoopPhase phase = PlanningPlayerLoopPhase.PreUpdate)
        {
            m_Service = service ?? throw new ArgumentNullException(nameof(service));

            if (!PlanningPlayerLoop.IsInstalled)
                PlanningPlayerLoop.Install(phase);

            PlanningPlayerLoop.Tick += Pump;
            m_Attached = true;
        }

        private void Pump() => m_Service.Pump();

        public void Dispose()
        {
            if (!m_Attached)
                return;

            PlanningPlayerLoop.Tick -= Pump;
            m_Attached = false;
        }
    }
}
