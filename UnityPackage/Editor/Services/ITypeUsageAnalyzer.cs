using System.Collections.Generic;

namespace AIInGames.Planning.Unity.Editor.Services
{
    /// <summary>
    /// Analyzes type usage throughout a domain.
    /// Helps prevent breaking changes when deleting or modifying types.
    /// </summary>
    public interface ITypeUsageAnalyzer
    {
        /// <summary>
        /// Finds all usages of a type in the domain.
        /// </summary>
        /// <param name="domain">The domain to search</param>
        /// <param name="typeName">The type to find usages of</param>
        /// <returns>List of human-readable usage descriptions</returns>
        List<string> FindTypeUsages(DomainAsset domain, string typeName);

        /// <summary>
        /// Checks if any types would be orphaned by deleting this type.
        /// </summary>
        /// <param name="domain">The domain to check</param>
        /// <param name="typeName">The type being deleted</param>
        /// <returns>True if deletion would orphan child types</returns>
        bool WouldOrphanTypes(DomainAsset domain, string typeName);
    }
}
