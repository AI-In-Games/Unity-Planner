namespace AIInGames.Planning.Runtime
{
    /// <summary>
    /// Counts unsatisfied goal conditions. Fast but gives no guidance in parameterized domains
    /// where most states are equally far from the goal by this measure.
    /// </summary>
    public readonly struct UnmetPredicateHeuristic : IHeuristic<PlanningState>
    {
        private readonly PlanningState m_Goal;

        internal UnmetPredicateHeuristic(ForwardDictionarySearchSpace space) : this(space.Goal) { }

        public UnmetPredicateHeuristic(PlanningState goal)
        {
            m_Goal = goal;
        }

        public float Compute(PlanningState state) => state.GetHeuristicDistance(m_Goal);
    }
}
