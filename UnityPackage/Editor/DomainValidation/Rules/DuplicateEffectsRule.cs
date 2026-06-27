using System.Collections.Generic;
using AIInGames.Planning.Unity;

namespace AIInGames.Planning.Unity.Editor.DomainValidation.Rules
{
    /// <summary>
    /// Reports effects that appear more than once with the same type (Add/Remove),
    /// predicate, and arguments. The second occurrence has no additional impact.
    /// </summary>
    public sealed class DuplicateEffectsRule : IDomainValidationRule
    {
        public IEnumerable<DomainIssue> Check(DomainAsset domain)
        {
            foreach (var action in domain.Actions)
            {
                var seen = new HashSet<string>();
                foreach (var effect in action.Effects.Effects)
                {
                    string sig = $"{effect.Type}:{effect.PredicateName}({string.Join(",", effect.Arguments)})";
                    if (!seen.Add(sig))
                        yield return DomainIssue.Warning(action.ActionName,
                            $"Action '{action.ActionName}': duplicate effect '{effect.PredicateName}' ({effect.Type}) — the second occurrence has no impact.");
                }
            }
        }
    }
}
