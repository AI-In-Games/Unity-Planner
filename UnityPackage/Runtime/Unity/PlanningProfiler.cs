using Unity.Profiling;

namespace AIInGames.Planning.Unity
{
    /// <summary>
    /// ProfilerMarker instances for the planning pipeline.
    /// Use with marker.Auto() or marker.Begin()/End() to see planning time in the Unity Profiler.
    /// These have zero overhead when the Profiler is not attached.
    /// </summary>
    public static class PlanningProfiler
    {
        public static readonly ProfilerMarker Plan          = new ProfilerMarker("AIPlanning.Plan");
        public static readonly ProfilerMarker GroundActions = new ProfilerMarker("AIPlanning.GroundActions");
    }
}
