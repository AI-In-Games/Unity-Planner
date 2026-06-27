using System.Collections.Generic;
using AIInGames.Planning.Unity;

namespace AIInGames.Planning.Unity.Editor.DomainValidation.Rules
{
    /// <summary>
    /// Reports conditions and effects that reference predicates not declared in the domain,
    /// use the wrong number of arguments, or reference arguments that are not parameters
    /// of the containing action.
    /// </summary>
    public sealed class UndefinedPredicateReferencesRule : IDomainValidationRule
    {
        public IEnumerable<DomainIssue> Check(DomainAsset domain)
        {
            var predicateMap = BuildPredicateMap(domain);

            foreach (var action in domain.Actions)
            {
                var paramNames = BuildParamSet(action);

                foreach (var issue in CheckNodes(action, action.Preconditions.Conditions, predicateMap, paramNames))
                    yield return issue;

                foreach (var effect in action.Effects.Effects)
                    foreach (var issue in CheckEffect(action, effect, predicateMap, paramNames))
                        yield return issue;
            }
        }

        private static Dictionary<string, PredicateDefinition> BuildPredicateMap(DomainAsset domain)
        {
            var map = new Dictionary<string, PredicateDefinition>(System.StringComparer.OrdinalIgnoreCase);
            foreach (var pred in domain.Predicates)
                if (!string.IsNullOrEmpty(pred.PredicateName))
                    map[pred.PredicateName] = pred;
            return map;
        }

        private static HashSet<string> BuildParamSet(ActionDefinition action)
        {
            var set = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            foreach (var p in action.Parameters)
                if (!string.IsNullOrEmpty(p.ParameterName))
                    set.Add(p.ParameterName);
            return set;
        }

        private static IEnumerable<DomainIssue> CheckNodes(
            ActionDefinition action,
            List<ConditionNode> nodes,
            Dictionary<string, PredicateDefinition> predicateMap,
            HashSet<string> paramNames)
        {
            foreach (var node in nodes)
            {
                if (node.Type == ConditionNode.NodeType.Predicate)
                {
                    foreach (var issue in CheckPredicateNode(action, node, predicateMap, paramNames))
                        yield return issue;
                }
                else
                {
                    foreach (var issue in CheckNodes(action, node.Children, predicateMap, paramNames))
                        yield return issue;
                }
            }
        }

        private static IEnumerable<DomainIssue> CheckPredicateNode(
            ActionDefinition action,
            ConditionNode node,
            Dictionary<string, PredicateDefinition> predicateMap,
            HashSet<string> paramNames)
        {
            if (string.IsNullOrEmpty(node.PredicateName)) yield break;

            if (!predicateMap.TryGetValue(node.PredicateName, out var def))
            {
                yield return DomainIssue.Error(action.ActionName,
                    $"Action '{action.ActionName}': condition references undefined predicate '{node.PredicateName}'.");
                yield break;
            }

            if (node.Arguments.Count != def.Parameters.Count)
                yield return DomainIssue.Error(action.ActionName,
                    $"Action '{action.ActionName}': condition '{node.PredicateName}' has {node.Arguments.Count} argument(s) but predicate expects {def.Parameters.Count}.");

            foreach (var arg in node.Arguments)
                if (!string.IsNullOrEmpty(arg) && !paramNames.Contains(arg))
                    yield return DomainIssue.Error(action.ActionName,
                        $"Action '{action.ActionName}': condition '{node.PredicateName}' uses '{arg}' which is not a parameter of this action.");
        }

        private static IEnumerable<DomainIssue> CheckEffect(
            ActionDefinition action,
            Effect effect,
            Dictionary<string, PredicateDefinition> predicateMap,
            HashSet<string> paramNames)
        {
            if (string.IsNullOrEmpty(effect.PredicateName)) yield break;

            if (!predicateMap.TryGetValue(effect.PredicateName, out var def))
            {
                yield return DomainIssue.Error(action.ActionName,
                    $"Action '{action.ActionName}': effect references undefined predicate '{effect.PredicateName}'.");
                yield break;
            }

            if (effect.Arguments.Count != def.Parameters.Count)
                yield return DomainIssue.Error(action.ActionName,
                    $"Action '{action.ActionName}': effect '{effect.PredicateName}' has {effect.Arguments.Count} argument(s) but predicate expects {def.Parameters.Count}.");

            foreach (var arg in effect.Arguments)
                if (!string.IsNullOrEmpty(arg) && !paramNames.Contains(arg))
                    yield return DomainIssue.Error(action.ActionName,
                        $"Action '{action.ActionName}': effect '{effect.PredicateName}' uses '{arg}' which is not a parameter of this action.");
        }
    }
}
