using System;
using System.Runtime.CompilerServices;

namespace AIInGames.Planning.Runtime
{
    /// <summary>
    /// Immutable planning state as a packed bit array. Each predicate occupies one bit at its registered index.
    /// Equality and hashing are O(wordCount), applicability and application are O(wordCount) bitwise operations.
    /// </summary>
    internal sealed class BitState : IEquatable<BitState>
    {
        internal readonly ulong[] Words;

        internal BitState(ulong[] words)
        {
            Words = words;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsApplicable(CompiledAction action)
        {
            for (int i = 0; i < Words.Length; i++)
            {
                if ((Words[i] & action.PrecondTrueMask[i]) != action.PrecondTrueMask[i])
                    return false;

                if ((Words[i] & action.PrecondFalseMask[i]) != 0)
                    return false;
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BitState Apply(CompiledAction action)
        {
            ulong[] newWords = new ulong[Words.Length];
            ApplyInto(action, newWords);
            return new BitState(newWords);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ApplyInto(CompiledAction action, ulong[] buffer)
        {
            for (int i = 0; i < Words.Length; i++)
                buffer[i] = (Words[i] | action.AddMask[i]) & ~action.RemoveMask[i];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Satisfies(ulong[] goalTrueMask, ulong[] goalFalseMask)
        {
            for (int i = 0; i < Words.Length; i++)
            {
                if ((Words[i] & goalTrueMask[i]) != goalTrueMask[i])
                    return false;

                if ((Words[i] & goalFalseMask[i]) != 0)
                    return false;
            }
            return true;
        }

        public bool Equals(BitState other)
        {
            if (other is null) return false;
            if (Words.Length != other.Words.Length) return false;

            for (int i = 0; i < Words.Length; i++)
            {
                if (Words[i] != other.Words[i]) return false;
            }
            return true;
        }

        public override bool Equals(object obj) => obj is BitState other && Equals(other);

        public override int GetHashCode()
        {
            HashCode hash = new HashCode();
            for (int i = 0; i < Words.Length; i++)
                hash.Add(Words[i]);
            return hash.ToHashCode();
        }
    }
}
