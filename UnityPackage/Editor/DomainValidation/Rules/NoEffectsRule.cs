using System.Collections.Generic;
using AIInGames.Planning.Unity;

namespace AIInGames.Planning.Unity.Editor.DomainValidation.Rules
{
    /// <summary>
    /// Reports actions that have no effects. Such actions never change the world state
    /// and will never contribute to reaching a goal in the planner.
    /// </summary>
    public sealed class NoEffectsRule : IDomainValidationRule
    {
        public IEnumerable<DomainIssue> Check(DomainAsset domain)
        {
            foreach (var action in domain.Actions)
                if (action.Effects.Effects.Count == 0)
                    yield return DomainIssue.Warning(action.ActionName,
                        $"Action '{action.ActionName}' has no effects and will never change the world state.");
        }
    }
}
