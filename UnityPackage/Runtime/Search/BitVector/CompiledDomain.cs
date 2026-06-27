using System.Collections.Generic;

namespace AIInGames.Planning.Runtime
{
    /// <summary>
    /// The result of compiling a planning domain into bit-vector form.
    /// Immutable after construction. Use DomainCompiler.Compile to obtain an instance,
    /// then pass it to Planner.Plan to skip recompilation on each planning call.
    /// </summary>
    public sealed class CompiledDomain
    {
        internal PredicateIndex PredicateIndex { get; }
        internal BitState InitialState { get; }
        internal ulong[] GoalTrueMask { get; }
        internal ulong[] GoalFalseMask { get; }
        internal int[] GoalTruePredicates { get; }
        internal int[] GoalFalsePredicates { get; }
        internal CompiledAction[] Actions { get; }
        internal GroundedAction[] OriginalActions { get; }
        internal int[][] AchieversForTrue { get; }
        internal int[][] AchieversForFalse { get; }

        internal CompiledDomain(
            PredicateIndex predicateIndex,
            BitState initialState,
            ulong[] goalTrueMask,
            ulong[] goalFalseMask,
            int[] goalTruePredicates,
            int[] goalFalsePredicates,
            CompiledAction[] actions,
            GroundedAction[] originalActions,
            int[][] achieversForTrue,
            int[][] achieversForFalse)
        {
            PredicateIndex = predicateIndex;
            InitialState = initialState;
            GoalTrueMask = goalTrueMask;
            GoalFalseMask = goalFalseMask;
            GoalTruePredicates = goalTruePredicates;
            GoalFalsePredicates = goalFalsePredicates;
            Actions = actions;
            OriginalActions = originalActions;
            AchieversForTrue = achieversForTrue;
            AchieversForFalse = achieversForFalse;
        }
    }
}
