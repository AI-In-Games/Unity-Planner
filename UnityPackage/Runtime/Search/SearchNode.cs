namespace AIInGames.Planning.Runtime
{
    /// <summary>
    /// One A* search node. Carries the state, the action index that produced it (or -1 for start),
    /// the parent pointer for plan reconstruction, and the g and f costs.
    /// Pool-friendly: fields are mutable so the node can be re-initialised after returning to a pool.
    /// </summary>
    internal sealed class SearchNode<TState>
    {
        public TState State;
        public int ActionIndex;
        public SearchNode<TState> Parent;
        public float GCost;
        public float FCost;

        public void Initialize(TState state, int actionIndex, SearchNode<TState> parent, float gCost, float hCost)
        {
            State = state;
            ActionIndex = actionIndex;
            Parent = parent;
            GCost = gCost;
            FCost = gCost + hCost;
        }
    }
}
