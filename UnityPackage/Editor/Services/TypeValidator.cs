using System.Linq;

namespace AIInGames.Planning.Unity.Editor.Services
{
    /// <summary>
    /// Validates type operations to ensure data integrity.
    /// Pure business logic - no UI dependencies, fully testable.
    /// </summary>
    public class TypeValidator : ITypeValidator
    {
        public bool WouldCreateCircularDependency(DomainAsset domain, string typeName, string newParent)
        {
            if (domain == null || string.IsNullOrEmpty(typeName) || string.IsNullOrEmpty(newParent))
                return false;

            if (newParent == "object")
                return false;

            if (newParent == typeName)
                return true; // Can't be own parent

            // Walk up the parent chain to see if typeName appears
            var current = newParent;
            while (!string.IsNullOrEmpty(current) && current != "object")
            {
                if (current == typeName)
                    return true;

                var parentType = domain.Types.FirstOrDefault(t => t.TypeName == current);
                current = parentType?.ParentType;
            }

            return false;
        }

        public string ValidateTypeName(DomainAsset domain, string typeName, TypeDefinition excludeType = null)
        {
            if (domain == null)
                return "Domain is null";

            if (string.IsNullOrWhiteSpace(typeName))
                return "Type name cannot be empty";

            if (typeName == "object")
                return "Cannot use reserved type name 'object'";

            if (typeName.Contains(" "))
                return "Type name cannot contain spaces";

            if (!char.IsLetter(typeName[0]) && typeName[0] != '_')
                return "Type name must start with a letter or underscore";

            // Check uniqueness
            var existing = domain.Types.FirstOrDefault(t => t.TypeName == typeName);
            if (existing != null && existing != excludeType)
                return $"Type '{typeName}' already exists";

            return null; // Valid
        }
    }
}
