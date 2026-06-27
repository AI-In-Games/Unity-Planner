using System;
using System.Collections.Generic;

namespace AIInGames.Planning.Runtime
{
    public sealed class PlannerRequest
    {
        internal PlannerRequest(
            int id,
            long sequence,
            PlanningProblemDefinition problem,
            int priority,
            string ownerKey,
            Action<PlanResult> onComplete)
        {
            Id = id;
            Sequence = sequence;
            Problem = problem ?? throw new ArgumentNullException(nameof(problem));
            Priority = priority;
            OwnerKey = ownerKey;
            OnComplete = onComplete;
        }

        public int Id { get; }
        public PlanningProblemDefinition Problem { get; }
        public int Priority { get; }
        public string OwnerKey { get; }
        public bool IsCancelled { get; internal set; }
        internal long Sequence { get; }

        /// <summary>Invoked with the plan once this request is dequeued and planned. Dropped if the request is cancelled.</summary>
        internal Action<PlanResult> OnComplete { get; }
    }

    /// <summary>
    /// Thread-safe priority queue for planning requests.
    /// Higher priority wins, then FIFO order within the same priority.
    /// </summary>
    public sealed class PlannerRequestQueue
    {
        private readonly object m_Gate = new object();
        private readonly List<PlannerRequest> m_Requests = new List<PlannerRequest>();
        private readonly Dictionary<int, PlannerRequest> m_ById = new Dictionary<int, PlannerRequest>();
        private readonly Dictionary<string, PlannerRequest> m_ByOwner =
            new Dictionary<string, PlannerRequest>(StringComparer.Ordinal);
        private int m_NextId = 1;
        private long m_NextSequence;

        public int Count
        {
            get
            {
                lock (m_Gate)
                {
                    int count = 0;
                    for (int i = 0; i < m_Requests.Count; i++)
                    {
                        if (!m_Requests[i].IsCancelled)
                            count++;
                    }
                    return count;
                }
            }
        }

        public PlannerRequest Submit(
            PlanningProblemDefinition problem,
            int priority = 0,
            string ownerKey = null,
            bool replaceOwner = false,
            Action<PlanResult> onComplete = null)
        {
            lock (m_Gate)
            {
                if (replaceOwner && !string.IsNullOrEmpty(ownerKey) &&
                    m_ByOwner.TryGetValue(ownerKey, out PlannerRequest previous))
                {
                    CancelLocked(previous);
                }

                PlannerRequest request = new PlannerRequest(
                    m_NextId++,
                    m_NextSequence++,
                    problem,
                    priority,
                    ownerKey,
                    onComplete);

                m_Requests.Add(request);
                m_ById[request.Id] = request;

                if (!string.IsNullOrEmpty(ownerKey))
                    m_ByOwner[ownerKey] = request;

                return request;
            }
        }

        public bool Cancel(int requestId)
        {
            lock (m_Gate)
            {
                if (!m_ById.TryGetValue(requestId, out PlannerRequest request))
                    return false;

                CancelLocked(request);
                return true;
            }
        }

        public bool TryDequeue(out PlannerRequest request)
        {
            lock (m_Gate)
            {
                int bestIndex = -1;
                PlannerRequest best = null;

                for (int i = 0; i < m_Requests.Count; i++)
                {
                    PlannerRequest candidate = m_Requests[i];
                    if (candidate.IsCancelled)
                        continue;

                    if (best == null ||
                        candidate.Priority > best.Priority ||
                        candidate.Priority == best.Priority && candidate.Sequence < best.Sequence)
                    {
                        best = candidate;
                        bestIndex = i;
                    }
                }

                if (bestIndex < 0)
                {
                    request = null;
                    CompactCancelledLocked();
                    return false;
                }

                m_Requests.RemoveAt(bestIndex);
                m_ById.Remove(best.Id);
                RemoveOwnerLocked(best);
                request = best;
                return true;
            }
        }

        public void Clear()
        {
            lock (m_Gate)
            {
                m_Requests.Clear();
                m_ById.Clear();
                m_ByOwner.Clear();
            }
        }

        private void CancelLocked(PlannerRequest request)
        {
            request.IsCancelled = true;
            m_ById.Remove(request.Id);
            RemoveOwnerLocked(request);
        }

        private void RemoveOwnerLocked(PlannerRequest request)
        {
            if (string.IsNullOrEmpty(request.OwnerKey))
                return;

            if (m_ByOwner.TryGetValue(request.OwnerKey, out PlannerRequest current) &&
                ReferenceEquals(current, request))
            {
                m_ByOwner.Remove(request.OwnerKey);
            }
        }

        private void CompactCancelledLocked()
        {
            for (int i = m_Requests.Count - 1; i >= 0; i--)
            {
                if (m_Requests[i].IsCancelled)
                    m_Requests.RemoveAt(i);
            }
        }
    }
}
