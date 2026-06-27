namespace AIInGames.Planning.Runtime
{
    public enum PlanInvalidationReason
    {
        None,
        ActionPreconditionsNotMet,
        GoalNotReachedByRemainingPlan
    }

    public readonly struct PlanValidationResult
    {
        public bool IsValid { get; }
        public PlanInvalidationReason Reason { get; }
        public int ActionIndex { get; }
        public GroundedAction Action { get; }
        public string FailedPredicateKey { get; }
        public bool ExpectedValue { get; }
        public bool ActualValue { get; }

        private PlanValidationResult(
            bool isValid,
            PlanInvalidationReason reason,
            int actionIndex,
            GroundedAction action,
            string failedPredicateKey,
            bool expectedValue,
            bool actualValue)
        {
            IsValid = isValid;
            Reason = reason;
            ActionIndex = actionIndex;
            Action = action;
            FailedPredicateKey = failedPredicateKey;
            ExpectedValue = expectedValue;
            ActualValue = actualValue;
        }

        public static PlanValidationResult Valid()
        {
            return new PlanValidationResult(true, PlanInvalidationReason.None, -1, null, null, false, false);
        }

        public static PlanValidationResult ActionPreconditionsNotMet(
            int actionIndex,
            GroundedAction action,
            string failedPredicateKey,
            bool expectedValue,
            bool actualValue)
        {
            return new PlanValidationResult(
                false,
                PlanInvalidationReason.ActionPreconditionsNotMet,
                actionIndex,
                action,
                failedPredicateKey,
                expectedValue,
                actualValue);
        }

        public static PlanValidationResult GoalNotReached(
            string failedPredicateKey,
            bool expectedValue,
            bool actualValue)
        {
            return new PlanValidationResult(
                false,
                PlanInvalidationReason.GoalNotReachedByRemainingPlan,
                -1,
                null,
                failedPredicateKey,
                expectedValue,
                actualValue);
        }
    }
}
