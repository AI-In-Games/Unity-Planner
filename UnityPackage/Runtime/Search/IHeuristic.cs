namespace AIInGames.Planning.Runtime
{
    /// <summary>
    /// Estimates the cost from the given state to whatever the search considers "goal."
    /// Implementations capture goal/initial/domain context at construction time and only see the
    /// current state at Compute() time. Lower values are closer.
    /// </summary>
    public interface IHeuristic<TState>
    {
        float Compute(TState state);
    }
}
