using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace AIInGames.Planning.Runtime
{
    /// <summary>
    /// Maps predicate names to stable bit indices for use in bit-vector state representation.
    /// Register all predicates before compiling a domain; indices must not change after compilation.
    /// </summary>
    internal sealed class PredicateIndex
    {
        /// <summary>Number of bits packed into one ulong storage word.</summary>
        public const int BitsPerWord = 64;

        // 1 << Log2BitsPerWord == BitsPerWord. Used to replace division by BitsPerWord with a shift.
        private const int Log2BitsPerWord = 6;

        // BitsPerWord - 1. Used to replace modulo by BitsPerWord with an AND.
        private const int BitWithinWordMask = BitsPerWord - 1;

        private readonly Dictionary<string, int> m_Indices;

        public int Count => m_Indices.Count;
        internal IEnumerable<string> Names => m_Indices.Keys;

        /// <summary>Number of 64-bit words needed to hold all registered predicates.</summary>
        public int WordCount => (Count + BitsPerWord - 1) / BitsPerWord;

        /// <summary>Word index that contains the given bit position. Same as bitIndex / BitsPerWord.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WordOf(int bitIndex) => bitIndex >> Log2BitsPerWord;

        /// <summary>Single-bit mask within the word for the given bit position. Same as 1UL &lt;&lt; (bitIndex % BitsPerWord).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong MaskOf(int bitIndex) => 1UL << (bitIndex & BitWithinWordMask);

        public PredicateIndex()
        {
            m_Indices = new Dictionary<string, int>(System.StringComparer.Ordinal);
        }

        /// <summary>
        /// Registers a predicate and returns its bit index. Idempotent — re-registering returns the same index.
        /// </summary>
        public int Register(string name)
        {
            if (m_Indices.TryGetValue(name, out int existing))
                return existing;

            int index = m_Indices.Count;
            m_Indices[name] = index;
            return index;
        }

        public bool TryGetIndex(string name, out int index)
        {
            return m_Indices.TryGetValue(name, out index);
        }
    }
}
