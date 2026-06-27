using System.Collections.Generic;

namespace AIInGames.Planning.Runtime
{
    /// <summary>
    /// The dictionary planner's compiled domain: the grounded actions for one object set, produced
    /// once and reused across replans. It parallels the bit-vector planner's CompiledDomain, but
    /// without a bitmask step, since the dictionary planner searches over PlanningState directly.
    /// The wrapper leaves room for future precomputation, such as an achiever index for the
    /// heuristic.
    /// </summary>
    public sealed class CompiledDictionaryDomain
    {
        public List<GroundedAction> Actions { get; }

        public CompiledDictionaryDomain(List<GroundedAction> actions)
        {
            Actions = actions;
        }
    }
}
