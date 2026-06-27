namespace AIInGames.Planning.Runtime
{
    /// <summary>
    /// Inadmissible one-step relaxed-plan heuristic over bit-vector states. For each unmet goal
    /// predicate, finds the achiever action with the fewest currently unmet preconditions and adds
    /// 1 + that count. Captures the compiled domain and goal masks at construction; subsequent
    /// Compute calls only see the current state.
    /// </summary>
    internal readonly struct HAddLiteBitHeuristic : IHeuristic<BitState>
    {
        private readonly CompiledAction[] m_Actions;
        private readonly int[][] m_AchieversForTrue;
        private readonly int[][] m_AchieversForFalse;
        private readonly int[] m_GoalTruePredicates;
        private readonly int[] m_GoalFalsePredicates;

        public HAddLiteBitHeuristic(BitVectorSearchSpace space)
        {
            m_Actions = space.Domain.Actions;
            m_AchieversForTrue = space.Domain.AchieversForTrue;
            m_AchieversForFalse = space.Domain.AchieversForFalse;
            m_GoalTruePredicates = space.GoalTruePredicates;
            m_GoalFalsePredicates = space.GoalFalsePredicates;
        }

        public float Compute(BitState state)
        {
            float h = 0;

            for (int gi = 0; gi < m_GoalTruePredicates.Length; gi++)
            {
                int pi = m_GoalTruePredicates[gi];
                int word = PredicateIndex.WordOf(pi);
                ulong bit = PredicateIndex.MaskOf(pi);

                if ((state.Words[word] & bit) != 0)
                    continue;

                int[] achievers = m_AchieversForTrue[pi];
                float bestCost = float.MaxValue / 2f;
                for (int i = 0; i < achievers.Length; i++)
                {
                    float cost = 1f + CountUnmetPreconditions(state, m_Actions[achievers[i]]);
                    if (cost < bestCost)
                    {
                        bestCost = cost;
                        if (bestCost <= 1f) break;
                    }
                }
                h += bestCost;
            }

            for (int gi = 0; gi < m_GoalFalsePredicates.Length; gi++)
            {
                int pi = m_GoalFalsePredicates[gi];
                int word = PredicateIndex.WordOf(pi);
                ulong bit = PredicateIndex.MaskOf(pi);

                if ((state.Words[word] & bit) == 0)
                    continue;

                int[] achievers = m_AchieversForFalse[pi];
                float bestCost = float.MaxValue / 2f;
                for (int i = 0; i < achievers.Length; i++)
                {
                    float cost = 1f + CountUnmetPreconditions(state, m_Actions[achievers[i]]);
                    if (cost < bestCost)
                    {
                        bestCost = cost;
                        if (bestCost <= 1f) break;
                    }
                }
                h += bestCost;
            }

            return h;
        }

        private static float CountUnmetPreconditions(BitState state, CompiledAction action)
        {
            int count = 0;
            for (int i = 0; i < state.Words.Length; i++)
            {
                count += BitMath.PopCount(~state.Words[i] & action.PrecondTrueMask[i]);
                count += BitMath.PopCount(state.Words[i] & action.PrecondFalseMask[i]);
            }
            return count;
        }
    }
}
