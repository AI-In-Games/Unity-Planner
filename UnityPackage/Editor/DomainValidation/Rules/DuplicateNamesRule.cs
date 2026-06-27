using System.Collections.Generic;
using AIInGames.Planning.Unity;

namespace AIInGames.Planning.Unity.Editor.DomainValidation.Rules
{
    /// <summary>
    /// Reports duplicate action names, predicate names, type names, and duplicate parameter
    /// names within a single action.
    /// </summary>
    public sealed class DuplicateNamesRule : IDomainValidationRule
    {
        public IEnumerable<DomainIssue> Check(DomainAsset domain)
        {
            var seen = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

            foreach (var action in domain.Actions)
            {
                if (!string.IsNullOrWhiteSpace(action.ActionName) && !seen.Add(action.ActionName))
                    yield return DomainIssue.Error(action.ActionName,
                        $"Duplicate action name '{action.ActionName}'.");
            }

            seen.Clear();
            foreach (var predicate in domain.Predicates)
            {
                if (!string.IsNullOrWhiteSpace(predicate.PredicateName) && !seen.Add(predicate.PredicateName))
                    yield return DomainIssue.Error(null,
                        $"Duplicate predicate name '{predicate.PredicateName}'.");
            }

            seen.Clear();
            foreach (var type in domain.Types)
            {
                if (!string.IsNullOrWhiteSpace(type.TypeName) && !seen.Add(type.TypeName))
                    yield return DomainIssue.Error(null,
                        $"Duplicate type name '{type.TypeName}'.");
            }

            foreach (var action in domain.Actions)
            {
                var paramSeen = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
                foreach (var param in action.Parameters)
                {
                    if (!string.IsNullOrWhiteSpace(param.ParameterName) && !paramSeen.Add(param.ParameterName))
                        yield return DomainIssue.Error(action.ActionName,
                            $"Action '{action.ActionName}' has duplicate parameter '{param.ParameterName}'.");
                }
            }
        }
    }
}
