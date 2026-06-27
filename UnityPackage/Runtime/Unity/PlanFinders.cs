using System.Collections.Generic;
using AIInGames.Planning.Unity;

namespace AIInGames.Planning.Runtime
{
    /// <summary>
    /// Builds plan finders for the common case. Choose the planner and heuristic once, here, and
    /// hand the result to PlannerService; callers then submit problems without knowing which
    /// planner runs. Both planners ground and compile once per object set and reuse the result
    /// across replans, differing only in representation.
    /// </summary>
    public static class PlanFinders
    {
        public static IPlanFinder BitVector(
            List<ActionDefinition> actions,
            PlannerHeuristicMode heuristic = PlannerHeuristicMode.HAddLite,
            int maxIterations = 10000)
        {
            return new CompiledPlanFinder<CompiledDomain>(
                actions, new BitVectorBackend(heuristic, maxIterations));
        }

        public static IPlanFinder Dictionary(
            List<ActionDefinition> actions,
            PlannerHeuristicMode heuristic = PlannerHeuristicMode.HAddLite,
            int maxIterations = 10000)
        {
            return new CompiledPlanFinder<CompiledDictionaryDomain>(
                actions, new DictionaryBackend(heuristic, maxIterations));
        }
    }
}
