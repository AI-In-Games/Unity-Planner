using System.Collections.Generic;
using AIInGames.Planning.Unity;

namespace AIInGames.Planning.Unity.Editor.DomainValidation.Rules
{
    /// <summary>
    /// Reports AND scopes that require the same predicate to be both true and false simultaneously.
    /// Such preconditions can never be satisfied, making the action unreachable.
    /// </summary>
    public sealed class ContradictoryConditionsRule : IDomainValidationRule
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
                var positives = new HashSet<string>();
                var negatives = new HashSet<string>();

                foreach (var node in nodes)
                {
                    if (node.Type == ConditionNode.NodeType.Predicate)
                        positives.Add(PredicateSignature(node));
                    else if (node.Type == ConditionNode.NodeType.Not
                             && node.Children.Count == 1
                             && node.Children[0].Type == ConditionNode.NodeType.Predicate)
                        negatives.Add(PredicateSignature(node.Children[0]));
                }

                foreach (var sig in positives)
                {
                    if (negatives.Contains(sig))
                        yield return DomainIssue.Error(action.ActionName,
                            $"Action '{action.ActionName}': impossible precondition — '{sig}' is required both true and false.");
                }
            }

            foreach (var node in nodes)
            {
                if (node.Type == ConditionNode.NodeType.And)
                    foreach (var issue in CheckScope(action, node.Children, true))
                        yield return issue;
                else if (node.Type == ConditionNode.NodeType.Or || node.Type == ConditionNode.NodeType.Not)
                    foreach (var issue in CheckScope(action, node.Children, false))
                        yield return issue;
            }
        }

        private static string PredicateSignature(ConditionNode node)
            => $"{node.PredicateName}({string.Join(",", node.Arguments)})";
    }
}
