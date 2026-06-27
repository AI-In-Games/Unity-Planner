using System.Runtime.CompilerServices;

namespace AIInGames.Planning.Runtime
{
    /// <summary>
    /// Bit-level primitives used by the bit-vector search path. Replaces the .NET 5+
    /// System.Numerics.BitOperations on runtimes that lack it (Unity 6 targets .NET Standard 2.1,
    /// which does not include BitOperations).
    /// </summary>
    internal static class BitMath
    {
        // SWAR popcount masks. Each mask isolates the low bits of every k-bit group across the
        // whole 64-bit word, so an AND extracts the low half and a shifted AND extracts the high
        // half. PopCount uses them to fold 1-counts upward: bits → pairs → nibbles → bytes.

        // Low bit of every 2-bit pair.    Pattern: 01 01 01 01 ...
        const ulong PairLowBits    = 0b01010101_01010101_01010101_01010101_01010101_01010101_01010101_01010101UL;

        // Low two bits of every nibble.   Pattern: 0011 0011 0011 0011 ...
        const ulong NibbleLowBits  = 0b00110011_00110011_00110011_00110011_00110011_00110011_00110011_00110011UL;

        // Low nibble of every byte.       Pattern: 00001111 00001111 ...
        const ulong ByteLowNibble  = 0b00001111_00001111_00001111_00001111_00001111_00001111_00001111_00001111UL;

        // One bit set per byte.           Pattern: 00000001 00000001 ...
        // Multiplying by this folds all byte values into the top byte.
        const ulong OneInEveryByte = 0b00000001_00000001_00000001_00000001_00000001_00000001_00000001_00000001UL;

        /// <summary>
        /// Counts the number of 1-bits in a 64-bit word (population count / Hamming weight).
        /// Uses the standard SWAR algorithm: sum 1-bits into 2-bit pairs, then 4-bit nibbles,
        /// then 8-bit bytes, then collapse all bytes into the top byte via a multiply trick.
        /// Six instructions, no branches.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int PopCount(ulong v)
        {
            // Step 1: each adjacent pair of bits becomes its own 2-bit count (0, 1, or 2).
            v -= (v >> 1) & PairLowBits;

            // Step 2: sum pairs into 4-bit nibble counts (0 through 4).
            v = (v & NibbleLowBits) + ((v >> 2) & NibbleLowBits);

            // Step 3: sum nibbles into 8-bit byte counts (0 through 8).
            v = (v + (v >> 4)) & ByteLowNibble;

            // Step 4: collapse byte counts into the top byte via multiply, then shift down.
            //   Multiplying by OneInEveryByte = 0x0101...01 makes the high byte hold the sum
            //   of all bytes, because each shifted copy of v in the partial products adds
            //   one more byte into the top byte. No byte overflows because each byte holds
            //   at most 8 (4 bits).
            return (int)((v * OneInEveryByte) >> 56);
        }
    }
}
