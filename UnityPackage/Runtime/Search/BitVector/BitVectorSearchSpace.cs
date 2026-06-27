using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace AIInGames.Planning.Runtime
{
    /// <summary>
    /// Forward search space backed by the bit-vector representation. Compiles the supplied domain
    /// once on construction; subsequent operations are bitwise ops on packed ulong arrays.
    /// Exposes the compiled domain internally for bit-vector heuristics that need achiever lists
    /// and goal masks.
    /// </summary>
    internal readonly struct BitVectorSearchSpace : ISearchSpace<BitState>
    {
        internal CompiledDomain Domain { get; }
        internal ulong[] GoalTrueMask { get; }
        internal ulong[] GoalFalseMask { get; }
        internal int[] GoalTruePredicates { get; }
        internal int[] GoalFalsePredicates { get; }

        private readonly BitState m_Initial;

        public BitState InitialState => m_Initial;
        public int ActionCount => Domain.Actions.Length;
        public bool ReverseDuringReconstruction => true;

        public BitVectorSearchSpace(PlanningState initial, PlanningState goal, List<GroundedAction> actions)
        {
            Domain = DomainCompiler.Compile(initial, goal, actions);
            m_Initial = Domain.InitialState;
            GoalTrueMask = Domain.GoalTrueMask;
            GoalFalseMask = Domain.GoalFalseMask;
            GoalTruePredicates = Domain.GoalTruePredicates;
            GoalFalsePredicates = Domain.GoalFalsePredicates;
        }

        /// <summary>Reuse a pre-compiled domain with possibly different initial / goal at search time.</summary>
        public BitVectorSearchSpace(CompiledDomain domain, PlanningState currentInitial, PlanningState currentGoal)
        {
            Domain = domain;
            m_Initial = DomainCompiler.CompileState(domain.PredicateIndex, currentInitial);
            DomainCompiler.BuildGoalMasks(domain.PredicateIndex, currentGoal,
                out ulong[] tMask, out ulong[] fMask, out int[] tPreds, out int[] fPreds);
            GoalTrueMask = tMask;
            GoalFalseMask = fMask;
            GoalTruePredicates = tPreds;
            GoalFalsePredicates = fPreds;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsGoal(BitState state) => state.Satisfies(GoalTrueMask, GoalFalseMask);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryApply(BitState state, int actionIndex, out BitState result)
        {
            CompiledAction action = Domain.Actions[actionIndex];
            if (!state.IsApplicable(action))
            {
                result = null;
                return false;
            }
            result = state.Apply(action);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Cost(int actionIndex) => Domain.Actions[actionIndex].Cost;

        public GroundedAction OriginalAction(int actionIndex) => Domain.OriginalActions[actionIndex];
    }
}
