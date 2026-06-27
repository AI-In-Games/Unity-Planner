#if PLANNING_DEBUG
namespace AIInGames.Planning.Runtime
{
    /// <summary>
    /// Opt-in diagnostics for the compiled-domain cache shared by the plan finders. Compiled in only
    /// when the PLANNING_DEBUG scripting symbol is defined. Enable it to log a line through
    /// PlannerLog.Info whenever a finder compiles a new object set (a miss) and to keep running hit
    /// and miss tallies. Counters are global across finders, so use it in a single scene while
    /// tuning. Leave PLANNING_DEBUG undefined in shipping builds and the whole feature disappears.
    /// </summary>
    public static class PlanCacheDiagnostics
    {
        public static bool Enabled;

        public static int Hits { get; private set; }
        public static int Misses { get; private set; }

        public static void Reset()
        {
            Hits = 0;
            Misses = 0;
        }

        internal static void RecordHit(string key)
        {
            if (!Enabled)
                return;
            Hits++;
        }

        internal static void RecordMiss(string key, int cachedCount)
        {
            if (!Enabled)
                return;
            Misses++;
            PlannerLog.Info($"[PlanCache] MISS: compiled a new object set ({cachedCount} cached, {Hits} hits so far): {key}");
        }
    }
}
#endif
