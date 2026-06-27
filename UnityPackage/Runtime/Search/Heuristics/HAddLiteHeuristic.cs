using System;
using System.Collections.Generic;

namespace AIInGames.Planning.Runtime
{
    /// <summary>
    /// One-step relaxed-plan heuristic over the dictionary-backed PlanningState representation.
    /// For each unmet goal predicate, finds the achiever action with the fewest currently unmet
    /// preconditions and returns 1 + that count. Sums across all goal predicates.
    /// Inadmissible: treats each goal predicate as independent, which overestimates when one
    /// action achieves multiple goals or when shared preconditions are counted separately.
    /// Significantly reduces node expansions vs goal-count in parameterized domains.
    /// </summary>
    public readonly struct HAddLiteHeuristic : IHeuristic<PlanningState>
    {
        private readonly Dictionary<string, List<GroundedAction>> m_Achievers;
        private readonly Dictionary<string, List<GroundedAction>> m_DeAchievers;
        private readonly PlanningState m_Goal;

        internal HAddLiteHeuristic(ForwardDictionarySearchSpace space)
            : this(space.Goal, space.Actions)
        {
        }

        public HAddLiteHeuristic(PlanningState goal, List<GroundedAction> actions)
        {
            m_Goal        = goal;
            m_Achievers   = new Dictionary<string, List<GroundedAction>>(StringComparer.Ordinal);
            m_DeAchievers = new Dictionary<string, List<GroundedAction>>(StringComparer.Ordinal);

            foreach (GroundedAction action in actions)
            {
                foreach (KeyValuePair<string, bool> effect in action.Effects.Predicates)
                {
                    Dictionary<string, List<GroundedAction>> table = effect.Value ? m_Achievers : m_DeAchievers;
                    if (!table.TryGetValue(effect.Key, out List<GroundedAction> list))
                    {
                        list = new List<GroundedAction>();
                        table[effect.Key] = list;
                    }
                    list.Add(action);
                }
            }
        }

        public float Compute(PlanningState state)
        {
            float h = 0;

            foreach (KeyValuePair<string, bool> goalPredicate in m_Goal.Predicates)
            {
                if (state.GetPredicate(goalPredicate.Key) == goalPredicate.Value)
                    continue;

                float bestCost = float.MaxValue / 2f;
                Dictionary<string, List<GroundedAction>> table = goalPredicate.Value ? m_Achievers : m_DeAchievers;

                if (table.TryGetValue(goalPredicate.Key, out List<GroundedAction> achievers))
                {
                    foreach (GroundedAction achiever in achievers)
                    {
                        float cost = 1f + state.GetHeuristicDistance(achiever.Preconditions);
                        if (cost < bestCost)
                        {
                            bestCost = cost;
                            if (bestCost <= 1f) break;
                        }
                    }
                }

                h += bestCost;
            }

            return h;
        }
    }
}
