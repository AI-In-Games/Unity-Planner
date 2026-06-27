using System.Collections.Generic;
using System.Linq;

namespace AIInGames.Planning.Unity.Editor.Services
{
    /// <summary>
    /// Analyzes type usage throughout a domain.
    /// Pure business logic - no UI dependencies, fully testable.
    /// </summary>
    public class TypeUsageAnalyzer : ITypeUsageAnalyzer
    {
        public List<string> FindTypeUsages(DomainAsset domain, string typeName)
        {
            if (domain == null || string.IsNullOrEmpty(typeName))
                return new List<string>();

            var usages = new List<string>();

            // Check child types
            var childTypes = domain.Types.Where(t => t.ParentType == typeName).ToList();
            foreach (var child in childTypes)
            {
                usages.Add($"• Type '{child.TypeName}' inherits from it");
            }

            // Check predicate parameters
            foreach (var predicate in domain.Predicates)
            {
                var paramUsages = predicate.Parameters.Where(p => p.ParameterType == typeName).ToList();
                if (paramUsages.Count > 0)
                {
                    usages.Add($"• Predicate '{predicate.PredicateName}' ({paramUsages.Count} parameter{(paramUsages.Count > 1 ? "s" : "")})");
                }
            }

            // Check action parameters
            foreach (var action in domain.Actions)
            {
                var paramUsages = action.Parameters.Where(p => p.ParameterType == typeName).ToList();
                if (paramUsages.Count > 0)
                {
                    usages.Add($"• Action '{action.ActionName}' ({paramUsages.Count} parameter{(paramUsages.Count > 1 ? "s" : "")})");
                }
            }

            return usages;
        }

        public bool WouldOrphanTypes(DomainAsset domain, string typeName)
        {
            if (domain == null || string.IsNullOrEmpty(typeName))
                return false;

            return domain.Types.Any(t => t.ParentType == typeName);
        }
    }
}
