namespace AIInGames.Planning.Runtime
{
    /// <summary>
    /// Finds a plan for a problem. Implementations own grounding, compilation, and search.
    /// "Find" is the search step and is distinct from <c>PlanExecutor</c>, which runs the
    /// resulting plan. The seam that lets <see cref="PlannerService"/> stay focused on
    /// queueing and dispatch.
    /// </summary>
    public interface IPlanFinder
    {
        PlanResult Find(PlanningProblemDefinition problem);
    }
}
