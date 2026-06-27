namespace AIInGames.Planning.Runtime
{
    /// <summary>
    /// Adapts an arbitrary IHeuristic&lt;TState&gt; class instance into a struct, so the engine's
    /// generic constraint (THeuristic : struct) is satisfied. Used by facades as the fallback when
    /// the caller supplied a heuristic of a type the facade doesn't know how to specialise on.
    ///
    /// Each Compute call dispatches through the interface — one virtual call. Slower than passing
    /// a struct heuristic directly, but recovers full devirtualisation for the engine's search-space
    /// calls, which are the dominant cost.
    /// </summary>
    internal readonly struct InterfaceHeuristic<TState> : IHeuristic<TState>
    {
        private readonly IHeuristic<TState> m_Inner;

        public InterfaceHeuristic(IHeuristic<TState> inner)
        {
            m_Inner = inner;
        }

        public float Compute(TState state) => m_Inner.Compute(state);
    }
}
