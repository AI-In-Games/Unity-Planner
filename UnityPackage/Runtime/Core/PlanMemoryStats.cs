namespace AIInGames.Planning.Runtime
{
    /// <summary>
    /// Structural memory counters captured from one planner search.
    /// Byte counts are estimates for managed x64 layouts, intended for
    /// planner-to-planner comparisons rather than process memory accounting.
    /// </summary>
    public struct PlanMemoryStats
    {
        public bool Available { get; }
        public bool UsesBitVector { get; }
        public int WordCount { get; }
        public int GeneratedStateCount { get; }
        public int CreatedNodeCount { get; }
        public int PeakOpenCount { get; }
        public int OpenListCapacity { get; }
        public int VisitedStateCount { get; }
        public int RentedNodeListCapacity { get; }
        public long SearchPeakEstimatedBytes { get; }

        public PlanMemoryStats(
            bool available,
            bool usesBitVector,
            int wordCount,
            int generatedStateCount,
            int createdNodeCount,
            int peakOpenCount,
            int openListCapacity,
            int visitedStateCount,
            int rentedNodeListCapacity,
            long searchPeakEstimatedBytes)
        {
            Available = available;
            UsesBitVector = usesBitVector;
            WordCount = wordCount;
            GeneratedStateCount = generatedStateCount;
            CreatedNodeCount = createdNodeCount;
            PeakOpenCount = peakOpenCount;
            OpenListCapacity = openListCapacity;
            VisitedStateCount = visitedStateCount;
            RentedNodeListCapacity = rentedNodeListCapacity;
            SearchPeakEstimatedBytes = searchPeakEstimatedBytes;
        }
    }
}
