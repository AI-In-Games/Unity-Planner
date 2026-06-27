using System.Collections.Generic;
using AIInGames.Planning.Unity;

namespace AIInGames.Planning.Runtime
{
    /// <summary>
    /// A planner-specific compile-and-search strategy. <c>Compile</c> turns lifted action schemas
    /// plus a problem's objects into a reusable compiled domain; <c>Search</c> solves one
    /// initial and goal pair over that compiled domain. <c>CompiledPlanFinder</c> caches the
    /// compiled domain per object set, so compilation happens once and is reused across replans.
    /// </summary>
    public interface IPlannerBackend<TCompiled>
    {
        TCompiled Compile(List<ActionDefinition> actions, IReadOnlyList<PlanningObject> objects);
        PlanResult Search(TCompiled compiled, PlanningState initial, PlanningState goal);
    }
}
