using System;

namespace AIInGames.Planning.Runtime
{
    internal sealed class PriorityQueue<T>
    {
        private T[] m_Items;
        private float[] m_Priorities;
        private int m_Count;

        public int Count => m_Count;
        internal int Capacity => m_Items.Length;

        public PriorityQueue(int initialCapacity = 16)
        {
            m_Items      = new T[initialCapacity];
            m_Priorities = new float[initialCapacity];
        }

        public void Enqueue(T item, float priority)
        {
            if (m_Count == m_Items.Length)
                Grow();

            m_Items[m_Count]      = item;
            m_Priorities[m_Count] = priority;
            SiftUp(m_Count);
            m_Count++;
        }

        public T Dequeue()
        {
            if (m_Count == 0)
                throw new InvalidOperationException("Queue is empty");

            T root = m_Items[0];
            m_Count--;

            if (m_Count > 0)
            {
                m_Items[0]      = m_Items[m_Count];
                m_Priorities[0] = m_Priorities[m_Count];
                SiftDown(0);
            }

            m_Items[m_Count] = default;
            return root;
        }

        public void Clear()
        {
            Array.Clear(m_Items, 0, m_Count);
            m_Count = 0;
        }

        private void SiftUp(int index)
        {
            float priority = m_Priorities[index];
            T item = m_Items[index];

            while (index > 0)
            {
                int parent = (index - 1) >> 1;
                if (m_Priorities[parent] <= priority)
                    break;

                m_Items[index]      = m_Items[parent];
                m_Priorities[index] = m_Priorities[parent];
                index = parent;
            }

            m_Items[index]      = item;
            m_Priorities[index] = priority;
        }

        private void SiftDown(int index)
        {
            float priority = m_Priorities[index];
            T item = m_Items[index];
            int half = m_Count >> 1;

            while (index < half)
            {
                int left  = (index << 1) + 1;
                int right = left + 1;
                int smallest = (right < m_Count && m_Priorities[right] < m_Priorities[left])
                    ? right : left;

                if (m_Priorities[smallest] >= priority)
                    break;

                m_Items[index]      = m_Items[smallest];
                m_Priorities[index] = m_Priorities[smallest];
                index = smallest;
            }

            m_Items[index]      = item;
            m_Priorities[index] = priority;
        }

        private void Grow()
        {
            int newCapacity = m_Items.Length == 0 ? 4 : m_Items.Length * 2;
            T[]     newItems      = new T[newCapacity];
            float[] newPriorities = new float[newCapacity];
            Array.Copy(m_Items,      newItems,      m_Count);
            Array.Copy(m_Priorities, newPriorities, m_Count);
            m_Items      = newItems;
            m_Priorities = newPriorities;
        }
    }
}
