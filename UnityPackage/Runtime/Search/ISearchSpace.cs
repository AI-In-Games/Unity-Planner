namespace AIInGames.Planning.Runtime
{
    /// <summary>
    /// The data and operations a search algorithm needs to explore a planning problem.
    /// Encapsulates the representation choice (bit-vector, dictionary, future SAS+) and the
    /// search direction (forward progression, backward regression) — the engine consumes this
    /// interface and does not know which is which.
    ///
    /// Actions are addressed by integer index to avoid copying struct action types in the inner loop.
    /// Applicability and application are combined into TryApply so backward search can short-circuit
    /// inconsistent subgoals without doing the regression work twice.
    /// </summary>
    public interface ISearchSpace<TState>
    {
        TState InitialState { get; }
        bool IsGoal(TState state);

        int ActionCount { get; }

        /// <summary>
        /// Apply the indexed action to the given state if applicable, returning the resulting state.
        /// Returns false if the action is not applicable or would produce an inconsistent state.
        /// </summary>
        bool TryApply(TState state, int actionIndex, out TState result);

        float Cost(int actionIndex);

        /// <summary>Maps an engine-internal action index back to the user-facing GroundedAction for plan reconstruction.</summary>
        GroundedAction OriginalAction(int actionIndex);

        /// <summary>
        /// Whether plan reconstruction should reverse the walked parent chain.
        /// Forward search walks from goal node back to start, so reversal yields execution order.
        /// Backward search walks from satisfied-subgoal-leaf back to the goal root, which already
        /// yields execution order — no reversal.
        /// </summary>
        bool ReverseDuringReconstruction { get; }
    }
}
