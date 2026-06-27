using System.Collections.Generic;
using AIInGames.Planning.Runtime;
using AIInGames.Planning.Unity;

namespace AIInGames.Planning.Unity.Editor
{
    /// <summary>
    /// Executes a planning query from text inputs. Separates planning logic from the editor window.
    /// </summary>
    public sealed class PlanRunner
    {
        public DomainAsset Domain         { get; set; }
        public string      ObjectsText    { get; set; } = "";
        public string      InitialText    { get; set; } = "";
        public string      GoalText       { get; set; } = "";

        public PlanRunResult Run()
        {
            if (Domain == null)
                return PlanRunResult.Failure("No domain assigned.");

            if (!PlanInputParser.TryParseObjects(ObjectsText, out List<PlanningObject> objects, out string objectsError))
                return PlanRunResult.Failure(objectsError);

            if (!PlanInputParser.TryParseState(InitialText, "initial state", out PlanningState initial, out string initialError))
                return PlanRunResult.Failure(initialError);

            if (!PlanInputParser.TryParseState(GoalText, "goal state", out PlanningState goal, out string goalError))
                return PlanRunResult.Failure(goalError);

            return Run(objects, initial, goal);
        }

        public PlanRunResult Run(List<PlanningObject> objects, PlanningState initial, PlanningState goal)
        {
            if (Domain == null)
                return PlanRunResult.Failure("No domain assigned.");

            List<GroundedAction> groundedActions = new ActionGrounder(objects).GroundAllActions(Domain.Actions);

            if (groundedActions.Count == 0)
                return PlanRunResult.Failure("No actions were grounded. Check that objects define names and types matching the domain.");

            PlanResult result = new ForwardBitVectorAStar().Plan(initial, goal, groundedActions);
            return PlanRunResult.Success(result, initial);
        }
    }

    public sealed class PlanRunResult
    {
        public string        ErrorMessage { get; }
        public PlanResult    PlanResult   { get; }
        public PlanningState InitialState { get; }

        public bool HasError => ErrorMessage != null;

        internal PlanRunResult(string error)
        {
            ErrorMessage = error;
        }

        internal PlanRunResult(PlanResult result, PlanningState initial)
        {
            PlanResult   = result;
            InitialState = initial;
        }

        public static PlanRunResult Failure(string error)                           => new PlanRunResult(error);
        public static PlanRunResult Success(PlanResult result, PlanningState init)  => new PlanRunResult(result, init);
    }
}
