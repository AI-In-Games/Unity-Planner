namespace AIInGames.Planning.Unity.Editor.Services
{
    /// <summary>
    /// Validates type operations to ensure data integrity.
    /// </summary>
    public interface ITypeValidator
    {
        /// <summary>
        /// Checks if setting a parent would create a circular dependency.
        /// </summary>
        /// <param name="domain">The domain containing types</param>
        /// <param name="typeName">The type whose parent is being changed</param>
        /// <param name="newParent">The proposed new parent</param>
        /// <returns>True if this would create a circular dependency</returns>
        bool WouldCreateCircularDependency(DomainAsset domain, string typeName, string newParent);

        /// <summary>
        /// Validates that a type name is valid and unique.
        /// </summary>
        /// <param name="domain">The domain to check</param>
        /// <param name="typeName">The proposed type name</param>
        /// <param name="excludeType">Type to exclude from uniqueness check (for renames)</param>
        /// <returns>Error message if invalid, null if valid</returns>
        string ValidateTypeName(DomainAsset domain, string typeName, TypeDefinition excludeType = null);
    }
}
