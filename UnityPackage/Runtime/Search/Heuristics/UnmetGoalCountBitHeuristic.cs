namespace AIInGames.Planning.Runtime
{
    /// <summary>
    /// Counts unsatisfied goal predicates in bit-vector form. Captures goal masks at construction
    /// and looks at the current state's words at Compute time.
    /// </summary>
    internal readonly struct UnmetGoalCountBitHeuristic : IHeuristic<BitState>
    {
        private readonly ulong[] m_GoalTrueMask;
        private readonly ulong[] m_GoalFalseMask;

        public UnmetGoalCountBitHeuristic(BitVectorSearchSpace space)
        {
            m_GoalTrueMask = space.GoalTrueMask;
            m_GoalFalseMask = space.GoalFalseMask;
        }

        public float Compute(BitState state)
        {
            int count = 0;
            for (int i = 0; i < state.Words.Length; i++)
            {
                count += BitMath.PopCount(~state.Words[i] & m_GoalTrueMask[i]);
                count += BitMath.PopCount(state.Words[i] & m_GoalFalseMask[i]);
            }
            return count;
        }
    }
}
