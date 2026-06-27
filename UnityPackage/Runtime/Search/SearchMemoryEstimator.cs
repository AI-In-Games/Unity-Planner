namespace AIInGames.Planning.Runtime
{
    /// <summary>
    /// Estimates peak memory usage for search results. Numbers are approximations of managed
    /// allocation sizes assuming a 64-bit runtime: every object pays a 16-byte header and pointers
    /// are 8 bytes. Useful for relative comparisons between domains, not for absolute accuracy.
    /// </summary>
    internal static class SearchMemoryEstimator
    {
        public static PlanMemoryStats CreateDictionaryMemoryStats(
            int createdNodeCount,
            int peakOpenCount,
            int openListCapacity,
            int peakClosedCount)
        {
            long searchPeakBytes = EstimateDictionarySearchBytes(
                createdNodeCount,
                openListCapacity,
                peakClosedCount);

            return new PlanMemoryStats(
                true,
                false,
                0,
                createdNodeCount,
                createdNodeCount,
                peakOpenCount,
                openListCapacity,
                peakClosedCount,
                0,
                searchPeakBytes);
        }

        private static long EstimateDictionarySearchBytes(int createdNodeCount, int openListCapacity, int closedCount)
        {
            long nodeBytes = ObjectBytes(8 + 8 + 8 + 4 + 4);
            long priorityQueueBytes = ObjectBytes(8 + 8 + 4) +
                                      ArrayBytes(openListCapacity, 8) +
                                      ArrayBytes(openListCapacity, 4);
            long closedSetBytes = DictionaryBytes(closedCount, 16);

            return createdNodeCount * nodeBytes + priorityQueueBytes + closedSetBytes;
        }

        private static long ObjectBytes(int fieldBytes) => Align8(16L + fieldBytes);

        private static long ArrayBytes(int length, int elementBytes)
        {
            if (length <= 0) return 0;
            return Align8(24L + (long)length * elementBytes);
        }

        private static long DictionaryBytes(int count, int entryBytes)
        {
            if (count <= 0) return ObjectBytes(8);
            int capacity = ApproxDictionaryCapacity(count);
            return ObjectBytes(8 + 8 + 4 + 4) +
                   ArrayBytes(capacity, 4) +
                   ArrayBytes(capacity, entryBytes);
        }

        private static int ApproxDictionaryCapacity(int count)
        {
            int target = (count * 4 + 2) / 3;
            int capacity = 4;
            while (capacity < target) capacity <<= 1;
            return capacity;
        }

        private static long Align8(long value) => (value + 7L) & ~7L;
    }
}
