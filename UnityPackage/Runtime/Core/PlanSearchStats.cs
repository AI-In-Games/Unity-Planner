namespace AIInGames.Planning.Runtime
{
    public struct PlanSearchStats
    {
        public bool Available { get; }
        public bool UsesBitVector { get; }
        public int ActionCount { get; }
        public int WordCount { get; }
        public int GoalPredicateCount { get; }
        public long ApplicabilityChecks { get; }
        public long ApplicableActions { get; }
        public long GeneratedStates { get; }
        public long DuplicateOrWorseStates { get; }
        public long StaleDequeues { get; }
        public long HeuristicEvaluations { get; }
        public long HeuristicGoalPredicateChecks { get; }
        public long HeuristicAchieverScans { get; }
        public long HeuristicPreconditionWordScans { get; }
        public int PeakOpenCount { get; }
        public int PeakVisitedStateCount { get; }

        public PlanSearchStats(
            bool available,
            bool usesBitVector,
            int actionCount,
            int wordCount,
            int goalPredicateCount,
            long applicabilityChecks,
            long applicableActions,
            long generatedStates,
            long duplicateOrWorseStates,
            long staleDequeues,
            long heuristicEvaluations,
            long heuristicGoalPredicateChecks,
            long heuristicAchieverScans,
            long heuristicPreconditionWordScans,
            int peakOpenCount,
            int peakVisitedStateCount)
        {
            Available = available;
            UsesBitVector = usesBitVector;
            ActionCount = actionCount;
            WordCount = wordCount;
            GoalPredicateCount = goalPredicateCount;
            ApplicabilityChecks = applicabilityChecks;
            ApplicableActions = applicableActions;
            GeneratedStates = generatedStates;
            DuplicateOrWorseStates = duplicateOrWorseStates;
            StaleDequeues = staleDequeues;
            HeuristicEvaluations = heuristicEvaluations;
            HeuristicGoalPredicateChecks = heuristicGoalPredicateChecks;
            HeuristicAchieverScans = heuristicAchieverScans;
            HeuristicPreconditionWordScans = heuristicPreconditionWordScans;
            PeakOpenCount = peakOpenCount;
            PeakVisitedStateCount = peakVisitedStateCount;
        }
    }
}
