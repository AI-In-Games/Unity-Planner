using System;
using System.Collections.Generic;

namespace AIInGames.Planning.Runtime
{
    internal class ValueStore<T>
    {
        private readonly Dictionary<Type, T> m_Values;

        public ValueStore()
        {
            m_Values = new Dictionary<Type, T>();
        }

        private ValueStore(Dictionary<Type, T> values)
        {
            m_Values = new Dictionary<Type, T>(values);
        }

        public void Set(Type keyType, T value)
        {
            m_Values[keyType] = value;
        }

        public bool TryGet(Type keyType, out T value)
        {
            return m_Values.TryGetValue(keyType, out value);
        }

        public bool Contains(Type keyType)
        {
            return m_Values.ContainsKey(keyType);
        }

        public IEnumerable<Type> Keys => m_Values.Keys;

        public void Clear()
        {
            m_Values.Clear();
        }

        public ValueStore<T> Clone()
        {
            return new ValueStore<T>(m_Values);
        }

        public int Count => m_Values.Count;
    }
}
