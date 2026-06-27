using System;
using System.Collections.Generic;

namespace AIInGames.Planning.Runtime
{
    public class PlanningState : IEquatable<PlanningState>
    {
        private readonly ValueStore<int> m_IntValues;
        private readonly ValueStore<float> m_FloatValues;
        private readonly ValueStore<string> m_StringValues;
        private readonly Dictionary<string, bool> m_Predicates;

        public PlanningState()
        {
            m_IntValues = new ValueStore<int>();
            m_FloatValues = new ValueStore<float>();
            m_StringValues = new ValueStore<string>();
            m_Predicates = new Dictionary<string, bool>(StringComparer.Ordinal);
        }

        private PlanningState(ValueStore<int> intValues, ValueStore<float> floatValues,
            ValueStore<string> stringValues, Dictionary<string, bool> predicates)
        {
            m_IntValues = intValues;
            m_FloatValues = floatValues;
            m_StringValues = stringValues;
            m_Predicates = predicates;
        }

        public void Set<T>(int value) where T : IntKey
        {
            m_IntValues.Set(typeof(T), value);
        }

        public void Set<T>(float value) where T : FloatKey
        {
            m_FloatValues.Set(typeof(T), value);
        }

        public void Set<T>(string value) where T : StringKey
        {
            m_StringValues.Set(typeof(T), value);
        }

        public int GetInt<T>() where T : IntKey
        {
            if (m_IntValues.TryGet(typeof(T), out int value))
                return value;
            return default;
        }

        public float GetFloat<T>() where T : FloatKey
        {
            if (m_FloatValues.TryGet(typeof(T), out float value))
                return value;
            return default;
        }

        public string GetString<T>() where T : StringKey
        {
            if (m_StringValues.TryGet(typeof(T), out string value))
                return value;
            return default;
        }

        public bool ContainsInt<T>() where T : IntKey
        {
            return m_IntValues.Contains(typeof(T));
        }

        public bool ContainsFloat<T>() where T : FloatKey
        {
            return m_FloatValues.Contains(typeof(T));
        }

        public bool ContainsString<T>() where T : StringKey
        {
            return m_StringValues.Contains(typeof(T));
        }

        public void SetPredicate(string name, bool value)
        {
            SetPredicateKey(name, value);
        }

        public void SetPredicate(string predicate, bool value, params string[] arguments)
        {
            SetPredicateKey(PlanningPredicate.BuildKey(predicate, arguments), value);
        }

        public void SetPredicate(PlanningPredicate predicate, bool value)
        {
            SetPredicateKey(predicate.Key, value);
        }

        public void SetPredicateKey(string key, bool value)
        {
            m_Predicates[PlanningPredicate.BuildKey(key)] = value;
        }

        public bool GetPredicate(string name)
        {
            return GetPredicateKey(name);
        }

        public bool GetPredicate(string predicate, params string[] arguments)
        {
            return GetPredicateKey(PlanningPredicate.BuildKey(predicate, arguments));
        }

        public bool GetPredicate(PlanningPredicate predicate)
        {
            return GetPredicateKey(predicate.Key);
        }

        public bool GetPredicateKey(string key)
        {
            if (m_Predicates.TryGetValue(PlanningPredicate.BuildKey(key), out bool value))
                return value;
            return false;
        }

        public bool ContainsPredicate(string name)
        {
            return ContainsPredicateKey(name);
        }

        public bool ContainsPredicate(string predicate, params string[] arguments)
        {
            return ContainsPredicateKey(PlanningPredicate.BuildKey(predicate, arguments));
        }

        public bool ContainsPredicate(PlanningPredicate predicate)
        {
            return ContainsPredicateKey(predicate.Key);
        }

        public bool ContainsPredicateKey(string key)
        {
            return m_Predicates.ContainsKey(PlanningPredicate.BuildKey(key));
        }

        public IEnumerable<KeyValuePair<string, bool>> Predicates => m_Predicates;

        public bool HasTypedValues =>
            m_IntValues.Count > 0 || m_FloatValues.Count > 0 || m_StringValues.Count > 0;

        public PlanningState Clone()
        {
            return new PlanningState(
                m_IntValues.Clone(),
                m_FloatValues.Clone(),
                m_StringValues.Clone(),
                new Dictionary<string, bool>(m_Predicates, StringComparer.Ordinal)
            );
        }

        public void ApplyEffects(PlanningState effects)
        {
            foreach (Type key in effects.m_IntValues.Keys)
            {
                if (effects.m_IntValues.TryGet(key, out int value))
                    m_IntValues.Set(key, value);
            }

            foreach (Type key in effects.m_FloatValues.Keys)
            {
                if (effects.m_FloatValues.TryGet(key, out float value))
                    m_FloatValues.Set(key, value);
            }

            foreach (Type key in effects.m_StringValues.Keys)
            {
                if (effects.m_StringValues.TryGet(key, out string value))
                    m_StringValues.Set(key, value);
            }

            foreach (KeyValuePair<string, bool> kvp in effects.m_Predicates)
            {
                m_Predicates[kvp.Key] = kvp.Value;
            }
        }

        public bool Satisfies(PlanningState goal)
        {
            foreach (Type key in goal.m_IntValues.Keys)
            {
                if (!m_IntValues.TryGet(key, out int currentValue) ||
                    !goal.m_IntValues.TryGet(key, out int goalValue) ||
                    currentValue != goalValue)
                {
                    return false;
                }
            }

            foreach (Type key in goal.m_FloatValues.Keys)
            {
                if (!m_FloatValues.TryGet(key, out float currentValue) ||
                    !goal.m_FloatValues.TryGet(key, out float goalValue) ||
                    Math.Abs(currentValue - goalValue) > 0.0001f)
                {
                    return false;
                }
            }

            foreach (Type key in goal.m_StringValues.Keys)
            {
                if (!m_StringValues.TryGet(key, out string currentValue) ||
                    !goal.m_StringValues.TryGet(key, out string goalValue) ||
                    currentValue != goalValue)
                {
                    return false;
                }
            }

            foreach (KeyValuePair<string, bool> kvp in goal.m_Predicates)
            {
                // Closed-world assumption: an absent predicate is false.
                // TryGetValue sets currentValue to false (default(bool)) when the key is missing.
                m_Predicates.TryGetValue(kvp.Key, out bool currentValue);
                if (currentValue != kvp.Value)
                    return false;
            }

            return true;
        }

        public int GetHeuristicDistance(PlanningState goal)
        {
            int distance = 0;

            foreach (Type key in goal.m_IntValues.Keys)
            {
                if (!m_IntValues.TryGet(key, out int currentValue) ||
                    !goal.m_IntValues.TryGet(key, out int goalValue))
                    distance++;
                else if (currentValue != goalValue)
                    distance++;
            }

            foreach (Type key in goal.m_FloatValues.Keys)
            {
                if (!m_FloatValues.TryGet(key, out float currentValue) ||
                    !goal.m_FloatValues.TryGet(key, out float goalValue))
                    distance++;
                else if (Math.Abs(currentValue - goalValue) > 0.0001f)
                    distance++;
            }

            foreach (Type key in goal.m_StringValues.Keys)
            {
                if (!m_StringValues.TryGet(key, out string currentValue) ||
                    !goal.m_StringValues.TryGet(key, out string goalValue))
                    distance++;
                else if (currentValue != goalValue)
                    distance++;
            }

            foreach (KeyValuePair<string, bool> kvp in goal.m_Predicates)
            {
                // Closed-world assumption: an absent predicate is false.
                m_Predicates.TryGetValue(kvp.Key, out bool currentValue);
                if (currentValue != kvp.Value)
                    distance++;
            }

            return distance;
        }

        public override int GetHashCode()
        {
            int hash = 0;

            foreach (Type key in m_IntValues.Keys)
                if (m_IntValues.TryGet(key, out int value))
                    hash ^= HashCode.Combine(key, value);

            foreach (Type key in m_FloatValues.Keys)
                if (m_FloatValues.TryGet(key, out float value))
                    hash ^= HashCode.Combine(key, value);

            foreach (Type key in m_StringValues.Keys)
                if (m_StringValues.TryGet(key, out string value))
                    hash ^= HashCode.Combine(key, value ?? string.Empty);

            foreach (KeyValuePair<string, bool> kvp in m_Predicates)
                hash ^= HashCode.Combine(kvp.Key, kvp.Value);

            return hash;
        }

        public bool Equals(PlanningState other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            if (m_Predicates.Count != other.m_Predicates.Count) return false;
            if (m_IntValues.Count != other.m_IntValues.Count) return false;
            if (m_FloatValues.Count != other.m_FloatValues.Count) return false;
            if (m_StringValues.Count != other.m_StringValues.Count) return false;

            return Satisfies(other);
        }

        public override bool Equals(object obj)
        {
            return obj is PlanningState other && Equals(other);
        }
    }
}
