using System.Collections.Generic;
using AIInGames.Planning.Unity;

namespace AIInGames.Planning.Runtime
{
    /// <summary>
    /// Dictionary backend: grounds the actions for an object set (a CompiledDictionaryDomain) and
    /// searches with forward dictionary A*. There is no bitmask compilation step; the grounded
    /// actions are the compiled form.
    /// </summary>
    public sealed class DictionaryBackend : IPlannerBackend<CompiledDictionaryDomain>
    {
        private readonly ForwardDictionaryAStar m_Planner;

        public DictionaryBackend(
            PlannerHeuristicMode heuristic = PlannerHeuristicMode.HAddLite,
            int maxIterations = 10000)
        {
            m_Planner = new ForwardDictionaryAStar(heuristic, DuplicateHandling.Dominance, maxIterations);
        }

        public CompiledDictionaryDomain Compile(List<ActionDefinition> actions, IReadOnlyList<PlanningObject> objects)
        {
            ActionGrounder grounder = new ActionGrounder(new List<PlanningObject>(objects));
            List<GroundedAction> grounded = grounder.GroundAllActions(actions);
            return new CompiledDictionaryDomain(grounded);
        }

        public PlanResult Search(CompiledDictionaryDomain compiled, PlanningState initial, PlanningState goal)
        {
            return m_Planner.Plan(initial, goal, compiled.Actions);
        }
    }
}
