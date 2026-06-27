using System.Collections.Generic;
using AIInGames.Planning.Unity;

namespace AIInGames.Planning.Unity.Editor.DomainValidation.Rules
{
    /// <summary>
    /// Reports actions, predicates, types, and parameters that have blank names.
    /// </summary>
    public sealed class EmptyNamesRule : IDomainValidationRule
    {
        public IEnumerable<DomainIssue> Check(DomainAsset domain)
        {
            foreach (var action in domain.Actions)
            {
                if (string.IsNullOrWhiteSpace(action.ActionName))
                    yield return DomainIssue.Error(null, "An action has an empty name.");

                foreach (var param in action.Parameters)
                    if (string.IsNullOrWhiteSpace(param.ParameterName))
                        yield return DomainIssue.Error(action.ActionName,
                            $"Action '{action.ActionName}' has a parameter with an empty name.");
            }

            foreach (var predicate in domain.Predicates)
            {
                if (string.IsNullOrWhiteSpace(predicate.PredicateName))
                    yield return DomainIssue.Error(null, "A predicate has an empty name.");

                foreach (var param in predicate.Parameters)
                    if (string.IsNullOrWhiteSpace(param.ParameterName))
                        yield return DomainIssue.Error(null,
                            $"Predicate '{predicate.PredicateName}' has a parameter with an empty name.");
            }

            foreach (var type in domain.Types)
                if (string.IsNullOrWhiteSpace(type.TypeName))
                    yield return DomainIssue.Error(null, "A type has an empty name.");
        }
    }
}
