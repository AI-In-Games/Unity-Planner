using System.Collections.Generic;
using AIInGames.Planning.Unity;

namespace AIInGames.Planning.Unity.Editor.DomainValidation.Rules
{
    /// <summary>
    /// Reports predicate conditions that appear more than once within the same AND scope.
    /// Duplicates are redundant — the planner evaluates the same fact twice for no benefit.
    /// </summary>
    public sealed class DuplicateConditionsRule : IDomainValidationRule
    {
        public IEnumerable<DomainIssue> Check(DomainAsset domain)
        {
            foreach (var action in domain.Actions)
            {
                bool rootIsAnd = action.Preconditions.Operator == ConditionGroup.LogicalOperator.And;
                foreach (var issue in CheckScope(action, action.Preconditions.Conditions, rootIsAnd))
                    yield return issue;
            }
        }

        private static IEnumerable<DomainIssue> CheckScope(
            ActionDefinition action,
            List<ConditionNode> nodes,
            bool inAnd)
        {
            if (inAnd)
            {
                var seen = new HashSet<string>();
                foreach (var node in nodes)
                {
                    if (node.Type != ConditionNode.NodeType.Predicate) continue;
                    string sig = PredicateSignature(node);
                    if (!seen.Add(sig))
                        yield return DomainIssue.Warning(action.ActionName,
                            $"Action '{action.ActionName}': condition '{node.PredicateName}' is listed more than once in the same AND block.");
                }
            }

            foreach (var node in nodes)
            {
                if (node.Type == ConditionNode.NodeType.And)
                    foreach (var issue in CheckScope(action, node.Children, true))
                        yield return issue;
                else if (node.Type == ConditionNode.NodeType.Or)
                    foreach (var issue in CheckScope(action, node.Children, false))
                        yield return issue;
                else if (node.Type == ConditionNode.NodeType.Not)
                    foreach (var issue in CheckScope(action, node.Children, false))
                        yield return issue;
            }
        }

        private static string PredicateSignature(ConditionNode node)
            => $"{node.PredicateName}({string.Join(",", node.Arguments)})";
    }
}
