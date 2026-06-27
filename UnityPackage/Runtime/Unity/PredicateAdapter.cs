using System;
using System.Collections.Generic;
using AiInGames.Blackboard;

namespace AIInGames.Planning.Runtime
{
    /// <summary>
    /// Converts rich blackboard data to boolean PDDL predicates.
    /// Registers evaluation rules as delegates for flexibility.
    /// </summary>
    public class PredicateAdapter
    {
        private readonly Dictionary<string, Func<Blackboard, bool>> m_Rules = new Dictionary<string, Func<Blackboard, bool>>(StringComparer.Ordinal);

        public void RegisterRule(string predicateName, Func<Blackboard, bool> rule)
        {
            if (string.IsNullOrEmpty(predicateName))
                throw new ArgumentException("Predicate name cannot be null or empty", nameof(predicateName));

            if (rule == null)
                throw new ArgumentNullException(nameof(rule));

            m_Rules[predicateName] = rule;
        }

        public Dictionary<string, bool> ExtractPredicates(Blackboard blackboard)
        {
            if (blackboard == null)
                throw new ArgumentNullException(nameof(blackboard));

            Dictionary<string, bool> predicates = new Dictionary<string, bool>(StringComparer.Ordinal);

            foreach (KeyValuePair<string, Func<Blackboard, bool>> kvp in m_Rules)
            {
                try
                {
                    bool value = kvp.Value(blackboard);
                    predicates[kvp.Key] = value;
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"Error evaluating predicate '{kvp.Key}': {e.Message}");
                    predicates[kvp.Key] = false;
                }
            }

            return predicates;
        }

        public bool RemoveRule(string predicateName)
        {
            return m_Rules.Remove(predicateName);
        }

        public void ClearRules()
        {
            m_Rules.Clear();
        }

        public int RuleCount => m_Rules.Count;

        public bool HasRule(string predicateName)
        {
            return m_Rules.ContainsKey(predicateName);
        }
    }
}
