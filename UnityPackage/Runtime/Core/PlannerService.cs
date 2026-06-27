using System;

namespace AIInGames.Planning.Runtime
{
    /// <summary>
    /// Drives a <see cref="PlannerRequestQueue"/>: agents submit problems, and each
    /// <see cref="Pump"/> plans exactly one request in priority order and delivers the plan
    /// to that request's callback. Submission is non-blocking; planning happens on whichever
    /// thread calls <see cref="Pump"/> (in Unity, the player-loop tick on the main thread).
    /// </summary>
    public sealed class PlannerService
    {
        private readonly IPlanFinder m_Finder;
        private readonly PlannerRequestQueue m_Queue = new PlannerRequestQueue();

        public PlannerService(IPlanFinder finder)
        {
            m_Finder = finder ?? throw new ArgumentNullException(nameof(finder));
        }

        /// <summary>Requests pending in the queue (excludes cancelled requests).</summary>
        public int PendingCount => m_Queue.Count;

        /// <summary>
        /// Queues a problem to be planned on a later <see cref="Pump"/>. Returns immediately
        /// with a handle; <paramref name="onComplete"/> fires once the plan is ready.
        /// When <paramref name="replaceOwner"/> is set, a still-pending request with the same
        /// <paramref name="ownerKey"/> is cancelled and its callback is dropped.
        /// </summary>
        public PlannerRequest Submit(
            PlanningProblemDefinition problem,
            Action<PlanResult> onComplete,
            int priority = 0,
            string ownerKey = null,
            bool replaceOwner = false)
        {
            if (problem == null)
                throw new ArgumentNullException(nameof(problem));

            return m_Queue.Submit(problem, priority, ownerKey, replaceOwner, onComplete);
        }

        /// <summary>Cancels a still-pending request so it is never planned. Returns false if unknown or already taken.</summary>
        public bool Cancel(int requestId) => m_Queue.Cancel(requestId);

        /// <summary>
        /// Plans the single highest-priority pending request and invokes its callback.
        /// Returns false when the queue is empty. Removes the request before planning, so a
        /// throwing callback leaves the queue intact for the next pump.
        /// </summary>
        public bool Pump()
        {
            if (!m_Queue.TryDequeue(out PlannerRequest request))
                return false;

            PlanResult result = m_Finder.Find(request.Problem);
            request.OnComplete?.Invoke(result);
            return true;
        }
    }
}
