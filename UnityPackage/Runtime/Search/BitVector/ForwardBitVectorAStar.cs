using System.Collections.Generic;

namespace AIInGames.Planning.Runtime
{
    /// <summary>
    /// Forward A* over the bit-vector representation. Thin facade: builds a BitVectorSearchSpace
    /// and the configured bit-vector heuristic, then constructs a specialised AStarEngine for the
    /// matching heuristic type so the JIT devirtualises every inner-loop call.
    /// </summary>
    public sealed class ForwardBitVectorAStar : ISearchEngine
    {
        private readonly PlannerHeuristicMode m_HeuristicMode;
        private readonly DuplicateHandling m_Duplicates;
        private readonly int m_MaxIterations;

        public ForwardBitVectorAStar(
            PlannerHeuristicMode heuristic = PlannerHeuristicMode.HAddLite,
            DuplicateHandling duplicates = DuplicateHandling.Dominance,
            int maxIterations = 10000)
        {
            m_HeuristicMode = heuristic;
            m_Duplicates = duplicates;
            m_MaxIterations = maxIterations;
        }

        public PlanResult Plan(PlanningState initial, PlanningState goal, List<GroundedAction> actions)
        {
            if (initial.HasTypedValues || goal.HasTypedValues)
            {
                PlannerLog.Warning(
                    "ForwardBitVectorAStar does not support typed values (IntKey, FloatKey, StringKey). " +
                    "Use ForwardDictionaryAStar for typed-value domains, or model numeric thresholds as boolean " +
                    "predicates via PredicateAdapter to keep the bit-vector path.");
                return PlanResult.Failure(0, 0, 0, default, default);
            }

            BitVectorSearchSpace space = new BitVectorSearchSpace(initial, goal, actions);
            return Run(space);
        }

        public PlanResult Plan(CompiledDomain compiledDomain, PlanningState currentInitial, PlanningState currentGoal)
        {
            BitVectorSearchSpace space = new BitVectorSearchSpace(compiledDomain, currentInitial, currentGoal);
            return Run(space);
        }

        private PlanResult Run(BitVectorSearchSpace space)
        {
            switch (m_HeuristicMode)
            {
                case PlannerHeuristicMode.HAddLite:
                    return new AStarEngine<BitVectorSearchSpace, BitState, HAddLiteBitHeuristic>(
                        space, new HAddLiteBitHeuristic(space), m_Duplicates, m_MaxIterations).Plan();

                case PlannerHeuristicMode.UnmetGoalCount:
                    return new AStarEngine<BitVectorSearchSpace, BitState, UnmetGoalCountBitHeuristic>(
                        space, new UnmetGoalCountBitHeuristic(space), m_Duplicates, m_MaxIterations).Plan();

                default:
                    return new AStarEngine<BitVectorSearchSpace, BitState, ZeroHeuristic<BitState>>(
                        space, default, m_Duplicates, m_MaxIterations).Plan();
            }
        }
    }
}
