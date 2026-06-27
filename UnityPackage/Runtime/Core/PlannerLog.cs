using System;

namespace AIInGames.Planning.Runtime
{
    /// <summary>
    /// Logging seam for the planning assemblies. Wire Warning up to UnityEngine.Debug.LogWarning
    /// (or your own logger) before planning runs. The Unity assembly does this automatically via
    /// PlanningPlayerLoop's static initializer.
    /// </summary>
    public static class PlannerLog
    {
        public static Action<string> Warning = message => Console.Error.WriteLine("[Planning] WARNING: " + message);

        public static Action<string> Info = message => Console.Out.WriteLine("[Planning] " + message);
    }
}
