using System.Collections.Generic;
using AIInGames.Planning.PDDL;

namespace AIInGames.Planning.Unity
{
    /// <summary>
    /// Converts a parsed PDDL domain into Unity ActionDefinition objects ready for grounding.
    /// Symmetric counterpart to PddlProblemImporter.
    /// </summary>
    public static class PddlDomainImporter
    {
        public static bool TryParse(
            string domainText,
            out List<ActionDefinition> actions,
            out string error)
        {
            PDDLParser parser = new PDDLParser();
            IParseResult<IDomain> result = parser.ParseDomain(domainText);
            if (!result.Success)
            {
                actions = null;
                error = result.Errors.Count > 0 ? result.Errors[0].Message : "PDDL domain parse failed.";
                return false;
            }

            return TryConvert(result.Result, out actions, out error);
        }

        public static bool TryConvert(
            IDomain source,
            out List<ActionDefinition> actions,
            out string error)
        {
            actions = new List<ActionDefinition>();
            error = null;

            HashSet<string> effectPredicates = PddlStaticPredicates.CollectEffectPredicateNames(source);

            for (int i = 0; i < source.Actions.Count; i++)
            {
                IAction src = source.Actions[i];
                ActionDefinition def = new ActionDefinition(Normalize(src.Name));

                for (int j = 0; j < src.Parameters.Count; j++)
                {
                    IParameter p = src.Parameters[j];
                    string type = p.Type != null
                        ? Normalize(p.Type.Name)
                        : InferParameterType(src.Precondition, p.Name, effectPredicates);
                    def.Parameters.Add(new ActionParameter(Normalize(p.Name), type));
                }

                if (!AddPreconditions(src.Precondition, def.Preconditions.Conditions, out error))
                    return false;

                if (!AddEffects(src.Effect, def.Effects.Effects, out error))
                    return false;

                actions.Add(def);
            }

            return true;
        }

        private static string Normalize(string value) => value.ToLowerInvariant();

        // A parameter's type is the unary predicate that constrains it in the precondition. When more
        // than one does, prefer a static one (a true type marker, never changed by an effect) over a
        // dynamic state predicate, so for example ?gripper resolves to "gripper", not "free".
        private static string InferParameterType(ICondition precondition, string paramName, HashSet<string> effectPredicates)
        {
            List<string> candidates = new List<string>();
            CollectTypeCandidates(precondition, paramName, candidates);

            for (int i = 0; i < candidates.Count; i++)
            {
                if (!effectPredicates.Contains(candidates[i]))
                    return candidates[i];
            }

            return candidates.Count > 0 ? candidates[0] : "object";
        }

        private static void CollectTypeCandidates(ICondition condition, string paramName, List<string> candidates)
        {
            if (condition.Type == ConditionType.Literal &&
                !condition.Literal.IsNegated &&
                condition.Literal.Arguments.Count == 1 &&
                Normalize(condition.Literal.Arguments[0]) == Normalize(paramName))
            {
                candidates.Add(Normalize(condition.Literal.Predicate.Name));
            }

            for (int i = 0; i < condition.Children.Count; i++)
                CollectTypeCandidates(condition.Children[i], paramName, candidates);
        }

        private static bool AddPreconditions(ICondition condition, List<ConditionNode> target, out string error)
        {
            error = null;
            switch (condition.Type)
            {
                case ConditionType.Literal:
                    target.Add(ToConditionNode(condition.Literal));
                    return true;

                case ConditionType.And:
                    for (int i = 0; i < condition.Children.Count; i++)
                    {
                        if (!AddPreconditions(condition.Children[i], target, out error))
                            return false;
                    }
                    return true;

                case ConditionType.Not:
                    if (condition.Children.Count == 1 && condition.Children[0].Type == ConditionType.Literal)
                    {
                        target.Add(ToConditionNode(condition.Children[0].Literal, forceNegated: true));
                        return true;
                    }
                    error = "unsupported nested not in precondition";
                    return false;

                default:
                    error = "unsupported precondition type: " + condition.Type;
                    return false;
            }
        }

        private static ConditionNode ToConditionNode(ILiteral literal, bool forceNegated = false)
        {
            var args = new List<string>(literal.Arguments.Count);
            for (int i = 0; i < literal.Arguments.Count; i++)
                args.Add(Normalize(literal.Arguments[i]));

            ConditionNode predicate = new ConditionNode
            {
                Type          = ConditionNode.NodeType.Predicate,
                PredicateName = Normalize(literal.Predicate.Name),
                Arguments     = args
            };

            if (!literal.IsNegated && !forceNegated)
                return predicate;

            ConditionNode not = new ConditionNode { Type = ConditionNode.NodeType.Not };
            not.Children.Add(predicate);
            return not;
        }

        private static bool AddEffects(IEffect effect, List<Effect> target, out string error)
        {
            error = null;
            switch (effect.Type)
            {
                case EffectType.Literal:
                    target.Add(ToEffect(effect.Literal));
                    return true;

                case EffectType.And:
                    for (int i = 0; i < effect.Children.Count; i++)
                    {
                        if (!AddEffects(effect.Children[i], target, out error))
                            return false;
                    }
                    return true;

                default:
                    error = "unsupported effect type: " + effect.Type;
                    return false;
            }
        }

        private static Effect ToEffect(ILiteral literal)
        {
            var args = new List<string>(literal.Arguments.Count);
            for (int i = 0; i < literal.Arguments.Count; i++)
                args.Add(Normalize(literal.Arguments[i]));

            return new Effect
            {
                Type          = literal.IsNegated ? Effect.EffectType.Remove : Effect.EffectType.Add,
                PredicateName = Normalize(literal.Predicate.Name),
                Arguments     = args
            };
        }
    }
}
