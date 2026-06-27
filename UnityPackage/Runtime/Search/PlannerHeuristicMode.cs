namespace AIInGames.Planning.Runtime
{
    /// <summary>
    /// Built-in heuristic selection for bit-vector search engines.
    /// </summary>
    public enum PlannerHeuristicMode
    {
        /// <summary>No heuristic: uniform-cost (Dijkstra) search.</summary>
        None,
        /// <summary>Goal-count: number of unsatisfied goal predicates. GOAP-equivalent baseline.</summary>
        UnmetGoalCount,
        /// <summary>hAdd-lite: inadmissible additive relaxation. Fewer node expansions at the cost of optimality.</summary>
        HAddLite
    }
}
