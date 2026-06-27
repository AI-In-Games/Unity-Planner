using System;
using System.Collections.Generic;
using AIInGames.Planning.PDDL;
using AIInGames.Planning.Runtime;

namespace AIInGames.Planning.Unity
{
    public static class PddlProblemImporter
    {
        /// <summary>
        /// Parses a problem whose objects declare their types in (:objects ...). For untyped STRIPS
        /// domains, where types are encoded as unary predicates in the init, use the overload that
        /// also takes the domain text so object types can be inferred.
        /// </summary>
        public static bool TryParse(
            string problemText,
            out PlanningProblemDefinition problem,
            out string error)
        {
            PDDLParser parser = new PDDLParser();
            IParseResult<IProblem> result = parser.ParseProblem(problemText);
            if (!result.Success)
            {
                problem = null;
                error = result.Errors.Count > 0 ? result.Errors[0].Message : "PDDL problem parse failed.";
                return false;
            }

            return TryConvert(result.Result, out problem, out error);
        }

        /// <summary>
        /// Parses a problem against its already-imported domain, inferring each object's type from
        /// the domain's static unary predicates (for example (ball b1) types b1 as "ball"). Use this
        /// for untyped STRIPS domains, where the single-argument overload would leave every object
        /// typed "object" and grounding would find no type-compatible objects for typed action
        /// parameters. Pass the actions you got from PddlDomainImporter.TryParse, which you need for
        /// grounding anyway, so the domain is not parsed twice.
        /// </summary>
        public static bool TryParse(
            string problemText,
            IReadOnlyList<ActionDefinition> domainActions,
            out PlanningProblemDefinition problem,
            out string error)
        {
            PDDLParser parser = new PDDLParser();
            IParseResult<IProblem> result = parser.ParseProblem(problemText);
            if (!result.Success)
            {
                problem = null;
                error = result.Errors.Count > 0 ? result.Errors[0].Message : "PDDL problem parse failed.";
                return false;
            }

            return TryConvert(result.Result, domainActions, out problem, out error);
        }

        public static bool TryConvert(
            IProblem source,
            out PlanningProblemDefinition problem,
            out string error)
        {
            return Convert(source, null, out problem, out error);
        }

        public static bool TryConvert(
            IProblem source,
            IReadOnlyList<ActionDefinition> domainActions,
            out PlanningProblemDefinition problem,
            out string error)
        {
            return Convert(source, PddlStaticPredicates.CollectEffectPredicateNames(domainActions), out problem, out error);
        }

        private static bool Convert(
            IProblem source,
            HashSet<string> effectPredicates,
            out PlanningProblemDefinition problem,
            out string error)
        {
            // When the domain is supplied and objects are untyped, infer each object's type from the
            // domain's static unary predicates in the init.
            Dictionary<string, string> inferredTypes = effectPredicates != null
                ? InferObjectTypes(source, effectPredicates)
                : null;

            PlanningProblemBuilder builder = PlanningProblemDefinition.Builder(source.Name);

            for (int i = 0; i < source.Objects.Count; i++)
            {
                IObject obj = source.Objects[i];
                string type;
                if (obj.Type != null)
                    type = obj.Type.Name.ToLowerInvariant();
                else if (inferredTypes != null && inferredTypes.TryGetValue(obj.Name, out string inferred))
                    type = inferred;
                else
                    type = "object";

                builder.Object(obj.Name, type);
            }

            for (int i = 0; i < source.InitialState.Count; i++)
            {
                ILiteral lit = source.InitialState[i];
                builder.InitialPredicate(lit.Predicate.Name, !lit.IsNegated, LiteralArguments(lit));
            }

            error = null;
            if (!AddGoal(source.Goal, builder, ref error))
            {
                problem = null;
                return false;
            }

            problem = builder.Build();
            return true;
        }

        // Each object's type is the first static unary predicate true of it in the init. Dynamic
        // unary predicates (state, for example (free g)) are skipped; binary predicates are not types.
        private static Dictionary<string, string> InferObjectTypes(IProblem source, HashSet<string> effectPredicates)
        {
            Dictionary<string, string> typeByObject = new Dictionary<string, string>(StringComparer.Ordinal);

            for (int i = 0; i < source.InitialState.Count; i++)
            {
                ILiteral lit = source.InitialState[i];
                if (lit.IsNegated || lit.Arguments.Count != 1)
                    continue;

                string predicate = lit.Predicate.Name.ToLowerInvariant();
                if (effectPredicates.Contains(predicate))
                    continue;

                string objectName = lit.Arguments[0];
                if (!typeByObject.ContainsKey(objectName))
                    typeByObject[objectName] = predicate;
            }

            return typeByObject;
        }

        private static bool AddGoal(ICondition condition, PlanningProblemBuilder builder, ref string error)
        {
            switch (condition.Type)
            {
                case ConditionType.Literal:
                    builder.GoalPredicate(
                        condition.Literal.Predicate.Name,
                        !condition.Literal.IsNegated,
                        LiteralArguments(condition.Literal));
                    return true;

                case ConditionType.And:
                    for (int i = 0; i < condition.Children.Count; i++)
                    {
                        if (!AddGoal(condition.Children[i], builder, ref error))
                            return false;
                    }
                    return true;

                case ConditionType.Not:
                    if (condition.Children.Count == 1 && condition.Children[0].Type == ConditionType.Literal)
                    {
                        ILiteral lit = condition.Children[0].Literal;
                        builder.GoalPredicate(lit.Predicate.Name, false, LiteralArguments(lit));
                        return true;
                    }
                    error = "Unsupported nested not in goal condition.";
                    return false;

                default:
                    error = $"Unsupported PDDL goal condition '{condition.Type}'. Only literals and conjunctions are supported.";
                    return false;
            }
        }

        private static string[] LiteralArguments(ILiteral literal)
        {
            string[] arguments = new string[literal.Arguments.Count];
            for (int i = 0; i < literal.Arguments.Count; i++)
                arguments[i] = literal.Arguments[i];
            return arguments;
        }
    }
}
