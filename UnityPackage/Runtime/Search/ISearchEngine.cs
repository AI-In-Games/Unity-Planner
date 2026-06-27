using System.Collections.Generic;

namespace AIInGames.Planning.Runtime
{
    /// <summary>
    /// A self-contained search algorithm and state representation. Engines are injected into
    /// IPlanner implementations to choose between representations (bit-vector, dictionary) and
    /// strategies (A*, future BFS/IDA*) without changing the planner.
    /// </summary>
    public interface ISearchEngine
    {
        PlanResult Plan(PlanningState initial, PlanningState goal, List<GroundedAction> actions);

        /// <summary>
        /// Plan using a pre-compiled domain. Engines that cannot exploit pre-compilation should
        /// extract the action list from the domain and run the standard search.
        /// </summary>
        PlanResult Plan(CompiledDomain compiledDomain, PlanningState currentInitial, PlanningState currentGoal);
    }
}
