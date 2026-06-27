using System.Collections.Generic;
using System.Text;
using AIInGames.Planning.PDDL;
using AIInGames.Planning.Runtime;

namespace AIInGames.Planning.Unity.Editor
{
    internal static class ProblemConverter
    {
        internal static List<PlanningObject> ToObjects(IProblem problem)
        {
            List<PlanningObject> objects = new List<PlanningObject>(problem.Objects.Count);
            foreach (IObject obj in problem.Objects)
                objects.Add(new PlanningObject(obj.Name, obj.Type?.Name ?? "object"));
            return objects;
        }

        internal static PlanningState ToInitialState(IProblem problem)
        {
            PlanningState state = new PlanningState();
            foreach (ILiteral lit in problem.InitialState)
                state.SetPredicate(LiteralKey(lit), !lit.IsNegated);
            return state;
        }

        internal static bool TryConvertGoal(ICondition condition, out PlanningState goal, out string error)
        {
            goal  = new PlanningState();
            error = null;
            return AddCondition(condition, goal, ref error);
        }

        private static bool AddCondition(ICondition cond, PlanningState goal, ref string error)
        {
            switch (cond.Type)
            {
                case ConditionType.Literal:
                    goal.SetPredicate(LiteralKey(cond.Literal), !cond.Literal.IsNegated);
                    return true;

                case ConditionType.And:
                    foreach (ICondition child in cond.Children)
                    {
                        if (!AddCondition(child, goal, ref error))
                            return false;
                    }
                    return true;

                default:
                    error = $"Unsupported goal condition type '{cond.Type}'. Only literal and conjunction (and) goals are supported.";
                    return false;
            }
        }

        private static string LiteralKey(ILiteral literal)
        {
            if (literal.Arguments.Count == 0)
                return literal.Predicate.Name;

            StringBuilder sb = new StringBuilder();
            sb.Append(literal.Predicate.Name);
            sb.Append('(');
            for (int i = 0; i < literal.Arguments.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(literal.Arguments[i]);
            }
            sb.Append(')');
            return sb.ToString();
        }
    }
}
