using System.Collections.Generic;
using AIInGames.Planning.Unity;

namespace AIInGames.Planning.Unity.Editor.DomainValidation.Rules
{
    /// <summary>
    /// Reports effects where the same predicate is both added and removed with identical
    /// arguments. The two effects cancel each other out, which is almost always a mistake.
    /// </summary>
    public sealed class CancelingEffectsRule : IDomainValidationRule
    {
        public IEnumerable<DomainIssue> Check(DomainAsset domain)
        {
            foreach (var action in domain.Actions)
            {
                var adds    = new HashSet<string>();
                var removes = new HashSet<string>();

                foreach (var effect in action.Effects.Effects)
                {
                    string sig = $"{effect.PredicateName}({string.Join(",", effect.Arguments)})";
                    if (effect.Type == Effect.EffectType.Add)    adds.Add(sig);
                    else                                          removes.Add(sig);
                }

                foreach (var sig in adds)
                {
                    if (removes.Contains(sig))
                        yield return DomainIssue.Error(action.ActionName,
                            $"Action '{action.ActionName}': effects cancel out — '{sig}' is both added and removed.");
                }
            }
        }
    }
}
