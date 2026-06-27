using System.Collections.Generic;
using AIInGames.Planning.Unity;

namespace AIInGames.Planning.Unity.Editor.DomainValidation.Rules
{
    /// <summary>
    /// Reports action parameters, predicate parameters, and type parents that reference
    /// a type name not declared in the domain. "object" is the implicit root type and is
    /// always valid.
    /// </summary>
    public sealed class UndefinedTypeReferencesRule : IDomainValidationRule
    {
        public IEnumerable<DomainIssue> Check(DomainAsset domain)
        {
            var knownTypes = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase) { "object" };
            foreach (var type in domain.Types)
                if (!string.IsNullOrWhiteSpace(type.TypeName))
                    knownTypes.Add(type.TypeName);

            foreach (var type in domain.Types)
            {
                if (!string.IsNullOrWhiteSpace(type.ParentType) && !knownTypes.Contains(type.ParentType))
                    yield return DomainIssue.Error(null,
                        $"Type '{type.TypeName}' has undefined parent type '{type.ParentType}'.");
            }

            foreach (var action in domain.Actions)
            {
                foreach (var param in action.Parameters)
                {
                    if (!string.IsNullOrWhiteSpace(param.ParameterType) && !knownTypes.Contains(param.ParameterType))
                        yield return DomainIssue.Error(action.ActionName,
                            $"Action '{action.ActionName}': parameter '{param.ParameterName}' uses undefined type '{param.ParameterType}'.");
                }
            }

            foreach (var predicate in domain.Predicates)
            {
                foreach (var param in predicate.Parameters)
                {
                    if (!string.IsNullOrWhiteSpace(param.ParameterType) && !knownTypes.Contains(param.ParameterType))
                        yield return DomainIssue.Error(null,
                            $"Predicate '{predicate.PredicateName}': parameter '{param.ParameterName}' uses undefined type '{param.ParameterType}'.");
                }
            }
        }
    }
}
