using System.Collections.Generic;

namespace AIInGames.Planning.Runtime
{
    public class PlanResult
    {
        public bool Success { get; }
        public List<GroundedAction> Actions { get; }
        public float TotalCost { get; }
        /// <summary>Unique states added to the closed set (nodes fully expanded).</summary>
        public int NodesExpanded { get; }
        /// <summary>Total dequeue calls including revisited states skipped by the closed-set check.</summary>
        public int Iterations { get; }
        /// <summary>Wall-clock time from the start of Plan() to the result being created, in milliseconds.</summary>
        public long ElapsedMs { get; }
        public PlanMemoryStats MemoryStats { get; }
        public PlanSearchStats SearchStats { get; }

#if DEBUG_PLAN
        /// <summary>
        /// Per-node expansion data collected during the search, capped at the Planner debug node cap.
        /// Only populated when the DEBUG_PLAN scripting define is set.
        /// </summary>
        public List<SearchNodeDebugData> DebugSearchNodes { get; } = new List<SearchNodeDebugData>();
#endif

        public PlanResult(bool success, List<GroundedAction> actions, float totalCost,
            int nodesExpanded, int iterations = 0, long elapsedMs = 0,
            PlanMemoryStats memoryStats = default(PlanMemoryStats),
            PlanSearchStats searchStats = default(PlanSearchStats))
        {
            Success = success;
            Actions = actions ?? new List<GroundedAction>();
            TotalCost = totalCost;
            NodesExpanded = nodesExpanded;
            Iterations = iterations;
            ElapsedMs = elapsedMs;
            MemoryStats = memoryStats;
            SearchStats = searchStats;
        }

        public static PlanResult Failure(int nodesExpanded, int iterations = 0, long elapsedMs = 0,
            PlanMemoryStats memoryStats = default(PlanMemoryStats),
            PlanSearchStats searchStats = default(PlanSearchStats))
        {
            return new PlanResult(false, null, 0, nodesExpanded, iterations, elapsedMs, memoryStats, searchStats);
        }

        public static PlanResult CreateSuccess(List<GroundedAction> actions, float totalCost,
            int nodesExpanded, int iterations = 0, long elapsedMs = 0,
            PlanMemoryStats memoryStats = default(PlanMemoryStats),
            PlanSearchStats searchStats = default(PlanSearchStats))
        {
            return new PlanResult(true, actions, totalCost, nodesExpanded, iterations, elapsedMs, memoryStats, searchStats);
        }
    }
}
