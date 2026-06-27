using System.Collections.Generic;

namespace AIInGames.Planning.Runtime
{
    /// <summary>
    /// Forward search space over the dictionary-backed PlanningState representation. Used when
    /// typed values (int/float/bool/string keys) make the bit-vector path inapplicable, or when
    /// the caller wants the dictionary path explicitly.
    /// </summary>
    internal readonly struct ForwardDictionarySearchSpace : ISearchSpace<PlanningState>
    {
        private readonly PlanningState m_Initial;
        private readonly PlanningState m_Goal;
        private readonly List<GroundedAction> m_Actions;

        public PlanningState InitialState => m_Initial;
        public int ActionCount => m_Actions.Count;
        public bool ReverseDuringReconstruction => true;

        internal List<GroundedAction> Actions => m_Actions;
        internal PlanningState Goal => m_Goal;

        public ForwardDictionarySearchSpace(PlanningState initial, PlanningState goal, List<GroundedAction> actions)
        {
            m_Initial = initial;
            m_Goal = goal;
            m_Actions = actions;
        }

        public bool IsGoal(PlanningState state) => state.Satisfies(m_Goal);

        public bool TryApply(PlanningState state, int actionIndex, out PlanningState result)
        {
            GroundedAction action = m_Actions[actionIndex];
            if (!action.IsApplicable(state))
            {
                result = null;
                return false;
            }
            result = action.Apply(state);
            return true;
        }

        public float Cost(int actionIndex) => m_Actions[actionIndex].Cost;

        public GroundedAction OriginalAction(int actionIndex) => m_Actions[actionIndex];
    }
}
