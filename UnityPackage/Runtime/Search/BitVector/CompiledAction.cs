namespace AIInGames.Planning.Runtime
{
    /// <summary>
    /// Bitmask representation of a grounded action. All arrays are indexed by word (ulong index).
    /// IsApplicable and Apply are pure bitwise operations — no dictionary lookups.
    /// </summary>
    internal readonly struct CompiledAction
    {
        public readonly int OriginalIndex;
        public readonly float Cost;
        public readonly ulong[] PrecondTrueMask;
        public readonly ulong[] PrecondFalseMask;
        public readonly ulong[] AddMask;
        public readonly ulong[] RemoveMask;

        public CompiledAction(
            int originalIndex,
            float cost,
            ulong[] precondTrueMask,
            ulong[] precondFalseMask,
            ulong[] addMask,
            ulong[] removeMask)
        {
            OriginalIndex = originalIndex;
            Cost = cost;
            PrecondTrueMask = precondTrueMask;
            PrecondFalseMask = precondFalseMask;
            AddMask = addMask;
            RemoveMask = removeMask;
        }
    }
}
