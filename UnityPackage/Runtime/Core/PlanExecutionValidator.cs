using System.Collections.Generic;

namespace AIInGames.Planning.Runtime
{
    public static class PlanExecutionValidator
    {
        public static PlanValidationResult ValidateRemainingPlan(
            PlanningState currentState,
            IReadOnlyList<GroundedAction> actions,
            int nextActionIndex,
            PlanningState goal)
        {
            if (currentState == null)
                throw new System.ArgumentNullException(nameof(currentState));
            if (actions == null)
                throw new System.ArgumentNullException(nameof(actions));
            if (goal == null)
                throw new System.ArgumentNullException(nameof(goal));

            PlanningState simulated = currentState.Clone();
            int start = nextActionIndex < 0 ? 0 : nextActionIndex;

            for (int i = start; i < actions.Count; i++)
            {
                GroundedAction action = actions[i];
                if (TryFindPredicateMismatch(
                        simulated,
                        action.Preconditions,
                        out string failedPredicate,
                        out bool expectedValue,
                        out bool actualValue))
                {
                    return PlanValidationResult.ActionPreconditionsNotMet(
                        i,
                        action,
                        failedPredicate,
                        expectedValue,
                        actualValue);
                }

                if (!simulated.Satisfies(action.Preconditions))
                {
                    return PlanValidationResult.ActionPreconditionsNotMet(
                        i,
                        action,
                        null,
                        false,
                        false);
                }

                simulated.ApplyEffects(action.Effects);
            }

            if (TryFindPredicateMismatch(
                    simulated,
                    goal,
                    out string failedGoalPredicate,
                    out bool goalExpected,
                    out bool goalActual))
            {
                return PlanValidationResult.GoalNotReached(
                    failedGoalPredicate,
                    goalExpected,
                    goalActual);
            }

            if (!simulated.Satisfies(goal))
                return PlanValidationResult.GoalNotReached(null, false, false);

            return PlanValidationResult.Valid();
        }

        public static bool IsNextActionStillApplicable(
            PlanningState currentState,
            IReadOnlyList<GroundedAction> actions,
            int nextActionIndex)
        {
            if (currentState == null || actions == null)
                return false;
            if (nextActionIndex < 0 || nextActionIndex >= actions.Count)
                return true;

            return currentState.Satisfies(actions[nextActionIndex].Preconditions);
        }

        private static bool TryFindPredicateMismatch(
            PlanningState current,
            PlanningState required,
            out string failedPredicateKey,
            out bool expectedValue,
            out bool actualValue)
        {
            foreach (KeyValuePair<string, bool> predicate in required.Predicates)
            {
                // GetPredicateKey returns false for absent keys (closed-world assumption).
                bool currentValue = current.GetPredicateKey(predicate.Key);
                if (currentValue != predicate.Value)
                {
                    failedPredicateKey = predicate.Key;
                    expectedValue = predicate.Value;
                    actualValue = currentValue;
                    return true;
                }
            }

            failedPredicateKey = null;
            expectedValue = false;
            actualValue = false;
            return false;
        }
    }
}
