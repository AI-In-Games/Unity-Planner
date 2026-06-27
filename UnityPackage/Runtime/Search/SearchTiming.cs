using System.Diagnostics;

namespace AIInGames.Planning.Runtime
{
    internal static class SearchTiming
    {
        public static long TicksToMs(long ticks)
        {
            return ticks * 1000L / Stopwatch.Frequency;
        }
    }
}
