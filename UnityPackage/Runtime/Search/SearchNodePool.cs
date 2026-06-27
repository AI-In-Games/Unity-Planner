using System.Collections.Generic;

namespace AIInGames.Planning.Runtime
{
    /// <summary>
    /// Pool of SearchNode instances reused across a single Plan() invocation to keep GC pressure down.
    /// Rent during search, return all rented nodes after the result is built.
    /// </summary>
    internal sealed class SearchNodePool<TState>
    {
        private readonly Stack<SearchNode<TState>> m_Available = new Stack<SearchNode<TState>>();

        public int ActiveCount { get; private set; }

        public SearchNode<TState> Rent(TState state, int actionIndex, SearchNode<TState> parent, float gCost, float hCost)
        {
            SearchNode<TState> node = m_Available.Count > 0 ? m_Available.Pop() : new SearchNode<TState>();
            node.Initialize(state, actionIndex, parent, gCost, hCost);
            ActiveCount++;
            return node;
        }

        public void Return(SearchNode<TState> node)
        {
            node.State = default;
            node.Parent = null;
            m_Available.Push(node);
            ActiveCount--;
        }
    }
}
