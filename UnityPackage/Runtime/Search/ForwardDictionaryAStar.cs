using System.Collections.Generic;

namespace AIInGames.Planning.Runtime
{
    /// <summary>
    /// Forward A* over the dictionary-backed PlanningState representation. Thin facade: builds a
    /// ForwardDictionarySearchSpace, dispatches to a specialised AStarEngine for known heuristic
    /// types, falls back to InterfaceHeuristic for user-provided custom heuristic instances.
    /// </summary>
    public sealed class ForwardDictionaryAStar : ISearchEngine
    {
        private readonly IHeuristic<PlanningState> m_Heuristic;
        private readonly PlannerHeuristicMode? m_Mode;
        private readonly DuplicateHandling m_Duplicates;
        private readonly int m_MaxIterations;

        public ForwardDictionaryAStar(
            IHeuristic<PlanningState> heuristic = null,
            DuplicateHandling duplicates = DuplicateHandling.ClosedSet,
            int maxIterations = 10000)
        {
            m_Heuristic = heuristic;
            m_Mode = null;
            m_Duplicates = duplicates;
            m_MaxIterations = maxIterations;
        }

        public ForwardDictionaryAStar(
            PlannerHeuristicMode heuristic,
            DuplicateHandling duplicates = DuplicateHandling.Dominance,
            int maxIterations = 10000)
        {
            m_Heuristic = null;
            m_Mode = heuristic;
            m_Duplicates = duplicates;
            m_MaxIterations = maxIterations;
        }

        public PlanResult Plan(PlanningState initial, PlanningState goal, List<GroundedAction> actions)
        {
            ForwardDictionarySearchSpace space = new ForwardDictionarySearchSpace(initial, goal, actions);

            if (m_Mode.HasValue)
                return RunByMode(space, m_Mode.Value);

            if (m_Heuristic == null)
                return Run(space, new HAddLiteHeuristic(space));

            if (m_Heuristic is HAddLiteHeuristic hAdd)
                return Run(space, hAdd);

            if (m_Heuristic is UnmetPredicateHeuristic)
                return Run(space, new UnmetPredicateHeuristic(space));

            return Run(space, new InterfaceHeuristic<PlanningState>(m_Heuristic));
        }

        public PlanResult Plan(CompiledDomain compiledDomain, PlanningState currentInitial, PlanningState currentGoal)
        {
            List<GroundedAction> actions = new List<GroundedAction>(compiledDomain.OriginalActions);
            return Plan(currentInitial, currentGoal, actions);
        }

        private PlanResult RunByMode(ForwardDictionarySearchSpace space, PlannerHeuristicMode mode)
        {
            switch (mode)
            {
                case PlannerHeuristicMode.HAddLite:
                    return Run(space, new HAddLiteHeuristic(space));
                case PlannerHeuristicMode.UnmetGoalCount:
                    return Run(space, new UnmetPredicateHeuristic(space));
                default:
                    return Run(space, new ZeroHeuristic<PlanningState>());
            }
        }

        private PlanResult Run<THeuristic>(ForwardDictionarySearchSpace space, THeuristic heuristic)
            where THeuristic : struct, IHeuristic<PlanningState>
        {
            return new AStarEngine<ForwardDictionarySearchSpace, PlanningState, THeuristic>(
                space, heuristic, m_Duplicates, m_MaxIterations).Plan();
        }
    }
}
