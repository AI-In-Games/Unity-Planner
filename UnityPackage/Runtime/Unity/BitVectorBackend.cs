using System.Collections.Generic;
using AIInGames.Planning.Unity;

namespace AIInGames.Planning.Runtime
{
    /// <summary>
    /// Bit-vector backend: grounds the actions for an object set and compiles them into bitmasks
    /// (a CompiledDomain), then searches with forward bit-vector A*.
    /// </summary>
    public sealed class BitVectorBackend : IPlannerBackend<CompiledDomain>
    {
        private readonly ForwardBitVectorAStar m_Planner;

        public BitVectorBackend(
            PlannerHeuristicMode heuristic = PlannerHeuristicMode.HAddLite,
            int maxIterations = 10000)
        {
            m_Planner = new ForwardBitVectorAStar(heuristic, DuplicateHandling.Dominance, maxIterations);
        }

        public CompiledDomain Compile(List<ActionDefinition> actions, IReadOnlyList<PlanningObject> objects)
        {
            ActionGrounder grounder = new ActionGrounder(new List<PlanningObject>(objects));
            List<GroundedAction> grounded = grounder.GroundAllActions(actions);
            // Empty seed states: the predicate index is built from the grounded actions, which
            // span every predicate reachable over this object set.
            return DomainCompiler.Compile(new PlanningState(), new PlanningState(), grounded);
        }

        public PlanResult Search(CompiledDomain compiled, PlanningState initial, PlanningState goal)
        {
            return m_Planner.Plan(compiled, initial, goal);
        }
    }
}
