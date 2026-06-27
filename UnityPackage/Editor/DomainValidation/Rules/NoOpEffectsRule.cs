using System.Collections.Generic;
using AIInGames.Planning.Unity;

namespace AIInGames.Planning.Unity.Editor.DomainValidation.Rules
{
    /// <summary>
    /// Reports effects that cannot change the world state because the preconditions already
    /// guarantee the target value: adding a predicate already required to be true, or
    /// removing one already required to be false.
    /// Only analysed for top-level AND conditions where truth values are unambiguous.
    /// </summary>
    public sealed class NoOpEffectsRule : IDomainValidationRule
    {
        public IEnumerable<DomainIssue> Check(DomainAsset domain)
        {
            foreach (var action in domain.Actions)
            {
                var requiredTrue  = new HashSet<string>();
                var requiredFalse = new HashSet<string>();

                bool rootIsAnd = action.Preconditions.Operator == ConditionGroup.LogicalOperator.And;
                CollectFacts(action.Preconditions.Conditions, rootIsAnd, requiredTrue, requiredFalse);

                foreach (var effect in action.Effects.Effects)
                {
                    string sig = $"{effect.PredicateName}({string.Join(",", effect.Arguments)})";
                    if (effect.Type == Effect.EffectType.Add && requiredTrue.Contains(sig))
                        yield return DomainIssue.Warning(action.ActionName,
                            $"Action '{action.ActionName}': effect adds '{effect.PredicateName}' which the precondition already requires to be true — this is a no-op.");
                    else if (effect.Type == Effect.EffectType.Remove && requiredFalse.Contains(sig))
                        yield return DomainIssue.Warning(action.ActionName,
                            $"Action '{action.ActionName}': effect removes '{effect.PredicateName}' which the precondition already requires to be false — this is a no-op.");
                }
            }
        }

        private static void CollectFacts(
            List<ConditionNode> nodes,
            bool inAnd,
            HashSet<string> requiredTrue,
            HashSet<string> requiredFalse)
        {
            if (!inAnd) return;

            foreach (var node in nodes)
            {
                if (node.Type == ConditionNode.NodeType.Predicate)
                    requiredTrue.Add($"{node.PredicateName}({string.Join(",", node.Arguments)})");
                else if (node.Type == ConditionNode.NodeType.Not
                         && node.Children.Count == 1
                         && node.Children[0].Type == ConditionNode.NodeType.Predicate)
                {
                    var child = node.Children[0];
                    requiredFalse.Add($"{child.PredicateName}({string.Join(",", child.Arguments)})");
                }
                else if (node.Type == ConditionNode.NodeType.And)
                    CollectFacts(node.Children, true, requiredTrue, requiredFalse);
            }
        }
    }
}
