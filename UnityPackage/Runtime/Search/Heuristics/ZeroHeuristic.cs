using System.Runtime.CompilerServices;

namespace AIInGames.Planning.Runtime
{
    /// <summary>
    /// Always returns 0. Used when the engine is configured without a heuristic, i.e. uniform-cost
    /// (Dijkstra) search. A struct so the JIT inlines the constant return into the engine loop and
    /// eliminates the heuristic call entirely.
    /// </summary>
    internal readonly struct ZeroHeuristic<TState> : IHeuristic<TState>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Compute(TState state) => 0f;
    }
}
