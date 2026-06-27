using System.Collections.Generic;
using System.Diagnostics;

namespace AIInGames.Planning.Runtime
{
    /// <summary>
    /// One A* loop. Works for any ISearchSpace&lt;TState&gt;: forward bit-vector or forward dictionary —
    /// the engine doesn't know which. Duplicate handling is selectable per call.
    ///
    /// TSpace is constrained to struct so the JIT specialises the engine per concrete space type,
    /// turning interface dispatches in the inner loop into direct calls. Search space implementations
    /// must therefore be readonly structs (see BitVectorSearchSpace, ForwardDictionarySearchSpace).
    /// </summary>
    internal sealed class AStarEngine<TSpace, TState, THeuristic>
        where TSpace : struct, ISearchSpace<TState>
        where THeuristic : struct, IHeuristic<TState>
    {
        private readonly TSpace m_Space;
        private readonly THeuristic m_Heuristic;
        private readonly DuplicateHandling m_Duplicates;
        private readonly int m_MaxIterations;
        private readonly SearchNodePool<TState> m_Pool = new SearchNodePool<TState>();
        private readonly List<SearchNode<TState>> m_Rented = new List<SearchNode<TState>>();

        public AStarEngine(
            TSpace space,
            THeuristic heuristic,
            DuplicateHandling duplicates,
            int maxIterations)
        {
            m_Space = space;
            m_Heuristic = heuristic;
            m_Duplicates = duplicates;
            m_MaxIterations = maxIterations;
        }

        public PlanResult Plan()
        {
            long startTick = Stopwatch.GetTimestamp();
            return m_Duplicates == DuplicateHandling.Dominance
                ? PlanWithDominance(startTick)
                : PlanWithClosedSet(startTick);
        }

        private PlanResult PlanWithClosedSet(long startTick)
        {
            PriorityQueue<SearchNode<TState>> openList = new PriorityQueue<SearchNode<TState>>();
            HashSet<TState> closedSet = new HashSet<TState>();
            m_Rented.Clear();

            TState initial = m_Space.InitialState;
            float h0 = m_Heuristic.Compute(initial);
            SearchNode<TState> startNode = Rent(initial, -1, null, 0, h0);
            openList.Enqueue(startNode, startNode.FCost);

            int nodesExpanded = 0;
            int iterations = 0;
            int peakOpenCount = openList.Count;
            int peakClosedCount = 0;
            long applicabilityChecks = 0;
            long applicableActions = 0;
            long generatedStates = 0;
            long duplicateStates = 0;
            long heuristicEvaluations = 1;
            SearchNode<TState> goalNode = null;

            while (openList.Count > 0 && nodesExpanded < m_MaxIterations)
            {
                SearchNode<TState> current = openList.Dequeue();
                iterations++;

                if (m_Space.IsGoal(current.State))
                {
                    goalNode = current;
                    break;
                }

                if (closedSet.Contains(current.State))
                    continue;

                closedSet.Add(current.State);
                nodesExpanded++;
                if (closedSet.Count > peakClosedCount)
                    peakClosedCount = closedSet.Count;

                int actionCount = m_Space.ActionCount;
                for (int i = 0; i < actionCount; i++)
                {
                    applicabilityChecks++;
                    if (!m_Space.TryApply(current.State, i, out TState newState))
                        continue;

                    applicableActions++;
                    generatedStates++;

                    if (closedSet.Contains(newState))
                    {
                        duplicateStates++;
                        continue;
                    }

                    float newGCost = current.GCost + m_Space.Cost(i);
                    heuristicEvaluations++;
                    float newHCost = m_Heuristic.Compute(newState);
                    SearchNode<TState> next = Rent(newState, i, current, newGCost, newHCost);
                    openList.Enqueue(next, next.FCost);
                    if (openList.Count > peakOpenCount)
                        peakOpenCount = openList.Count;
                }
            }

            return Finalize(
                goalNode, nodesExpanded, iterations, startTick,
                m_Rented.Count, peakOpenCount, openList.Capacity, peakClosedCount,
                applicabilityChecks, applicableActions, generatedStates, duplicateStates,
                staleDequeues: 0, heuristicEvaluations);
        }

        private PlanResult PlanWithDominance(long startTick)
        {
            PriorityQueue<SearchNode<TState>> openList = new PriorityQueue<SearchNode<TState>>();
            Dictionary<TState, float> bestGCost = new Dictionary<TState, float>();
            m_Rented.Clear();

            TState initial = m_Space.InitialState;
            float h0 = m_Heuristic.Compute(initial);
            SearchNode<TState> startNode = Rent(initial, -1, null, 0, h0);
            bestGCost[initial] = 0f;
            openList.Enqueue(startNode, startNode.FCost);

            int nodesExpanded = 0;
            int iterations = 0;
            int peakOpenCount = openList.Count;
            int peakVisitedStateCount = bestGCost.Count;
            long applicabilityChecks = 0;
            long applicableActions = 0;
            long generatedStates = 0;
            long duplicateOrWorseStates = 0;
            long staleDequeues = 0;
            long heuristicEvaluations = 1;
            SearchNode<TState> goalNode = null;

            while (openList.Count > 0 && nodesExpanded < m_MaxIterations)
            {
                SearchNode<TState> current = openList.Dequeue();
                iterations++;

                if (current.GCost > bestGCost[current.State])
                {
                    staleDequeues++;
                    continue;
                }

                if (m_Space.IsGoal(current.State))
                {
                    goalNode = current;
                    break;
                }

                nodesExpanded++;

                int actionCount = m_Space.ActionCount;
                for (int i = 0; i < actionCount; i++)
                {
                    applicabilityChecks++;
                    if (!m_Space.TryApply(current.State, i, out TState newState))
                        continue;

                    applicableActions++;
                    generatedStates++;
                    float newGCost = current.GCost + m_Space.Cost(i);

                    if (bestGCost.TryGetValue(newState, out float existingG) && existingG <= newGCost)
                    {
                        duplicateOrWorseStates++;
                        continue;
                    }

                    bestGCost[newState] = newGCost;
                    if (bestGCost.Count > peakVisitedStateCount)
                        peakVisitedStateCount = bestGCost.Count;

                    heuristicEvaluations++;
                    float newHCost = m_Heuristic.Compute(newState);
                    SearchNode<TState> next = Rent(newState, i, current, newGCost, newHCost);
                    openList.Enqueue(next, next.FCost);
                    if (openList.Count > peakOpenCount)
                        peakOpenCount = openList.Count;
                }
            }

            return Finalize(
                goalNode, nodesExpanded, iterations, startTick,
                m_Rented.Count, peakOpenCount, openList.Capacity, peakVisitedStateCount,
                applicabilityChecks, applicableActions, generatedStates, duplicateOrWorseStates,
                staleDequeues, heuristicEvaluations);
        }

        private SearchNode<TState> Rent(TState state, int actionIndex, SearchNode<TState> parent, float gCost, float hCost)
        {
            SearchNode<TState> node = m_Pool.Rent(state, actionIndex, parent, gCost, hCost);
            m_Rented.Add(node);
            return node;
        }

        private PlanResult Finalize(
            SearchNode<TState> goalNode, int nodesExpanded, int iterations, long startTick,
            int createdNodeCount, int peakOpenCount, int openListCapacity, int peakVisitedStateCount,
            long applicabilityChecks, long applicableActions, long generatedStates,
            long duplicateOrWorseStates, long staleDequeues, long heuristicEvaluations)
        {
            bool found = goalNode != null;
            float successCost = found ? goalNode.GCost : 0;
            List<GroundedAction> plan = found ? ReconstructPlan(goalNode) : null;

            PlanMemoryStats memoryStats = SearchMemoryEstimator.CreateDictionaryMemoryStats(
                createdNodeCount, peakOpenCount, openListCapacity, peakVisitedStateCount);

            PlanSearchStats searchStats = new PlanSearchStats(
                true,
                false,
                m_Space.ActionCount,
                0,
                0,
                applicabilityChecks,
                applicableActions,
                generatedStates,
                duplicateOrWorseStates,
                staleDequeues,
                heuristicEvaluations,
                0,
                0,
                0,
                peakOpenCount,
                peakVisitedStateCount);

            for (int i = 0; i < m_Rented.Count; i++)
                m_Pool.Return(m_Rented[i]);

            long elapsedMs = SearchTiming.TicksToMs(Stopwatch.GetTimestamp() - startTick);
            return found
                ? PlanResult.CreateSuccess(plan, successCost, nodesExpanded, iterations, elapsedMs, memoryStats, searchStats)
                : PlanResult.Failure(nodesExpanded, iterations, elapsedMs, memoryStats, searchStats);
        }

        private List<GroundedAction> ReconstructPlan(SearchNode<TState> goalNode)
        {
            List<GroundedAction> plan = new List<GroundedAction>();
            SearchNode<TState> current = goalNode;
            while (current.Parent != null)
            {
                plan.Add(m_Space.OriginalAction(current.ActionIndex));
                current = current.Parent;
            }
            if (m_Space.ReverseDuringReconstruction)
                plan.Reverse();
            return plan;
        }
    }
}
