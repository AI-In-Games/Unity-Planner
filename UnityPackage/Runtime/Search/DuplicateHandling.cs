namespace AIInGames.Planning.Runtime
{
    /// <summary>
    /// Strategy for handling already-visited states during search.
    /// </summary>
    public enum DuplicateHandling
    {
        /// <summary>
        /// Mark each expanded state closed; never re-expand. Optimal only under admissible and
        /// consistent heuristics. Cheaper bookkeeping and typically faster when the guarantee holds.
        /// </summary>
        ClosedSet,

        /// <summary>
        /// Track best g-cost per state; re-enqueue when a cheaper path is found and skip stale dequeues.
        /// Required for correctness under inadmissible or inconsistent heuristics.
        /// </summary>
        Dominance
    }
}
