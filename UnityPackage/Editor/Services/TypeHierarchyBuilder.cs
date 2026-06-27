using System.Collections.Generic;

namespace AIInGames.Planning.Unity.Editor.Services
{
    /// <summary>
    /// A node in the displayed type hierarchy. The root node represents the implicit "object" type
    /// and has a null <see cref="Type"/>; every other node carries its <see cref="TypeDefinition"/>.
    /// </summary>
    public sealed class TypeHierarchyNode
    {
        public string Name { get; }
        public TypeDefinition Type { get; }
        public List<TypeHierarchyNode> Children { get; } = new List<TypeHierarchyNode>();

        public bool IsRoot => Type == null;

        public TypeHierarchyNode(string name, TypeDefinition type)
        {
            Name = name;
            Type = type;
        }
    }

    /// <summary>
    /// Builds the type tree shown in the domain editor. "object" is the implicit root of every PDDL
    /// type hierarchy, so it is always the single root node, with the domain's declared types nested
    /// beneath it by parent. Untyped domains (no declared types) yield a root with no children rather
    /// than an empty tree. Pure business logic, no UI dependencies, so it is unit-testable.
    /// </summary>
    public static class TypeHierarchyBuilder
    {
        public const string RootName = "object";

        public static TypeHierarchyNode Build(IReadOnlyList<TypeDefinition> types)
        {
            TypeHierarchyNode root = new TypeHierarchyNode(RootName, null);
            if (types == null)
                return root;

            HashSet<string> known = new HashSet<string>(System.StringComparer.Ordinal);
            Dictionary<string, List<TypeDefinition>> childrenOf =
                new Dictionary<string, List<TypeDefinition>>(System.StringComparer.Ordinal);

            foreach (TypeDefinition type in types)
            {
                if (!IsDeclaredType(type))
                    continue;
                known.Add(type.TypeName);
                if (!childrenOf.ContainsKey(type.TypeName))
                    childrenOf[type.TypeName] = new List<TypeDefinition>();
            }

            List<TypeDefinition> topLevel = new List<TypeDefinition>();
            foreach (TypeDefinition type in types)
            {
                if (!IsDeclaredType(type))
                    continue;

                string parent = string.IsNullOrEmpty(type.ParentType) ? RootName : type.ParentType;
                if (parent == RootName || !known.Contains(parent))
                    topLevel.Add(type);
                else
                    childrenOf[parent].Add(type);
            }

            HashSet<string> visited = new HashSet<string>(System.StringComparer.Ordinal);
            foreach (TypeDefinition type in topLevel)
                root.Children.Add(BuildNode(type, childrenOf, visited));

            return root;
        }

        private static TypeHierarchyNode BuildNode(
            TypeDefinition type,
            Dictionary<string, List<TypeDefinition>> childrenOf,
            HashSet<string> visited)
        {
            TypeHierarchyNode node = new TypeHierarchyNode(type.TypeName, type);
            if (!visited.Add(type.TypeName))
                return node;

            if (childrenOf.TryGetValue(type.TypeName, out List<TypeDefinition> children))
                foreach (TypeDefinition child in children)
                    node.Children.Add(BuildNode(child, childrenOf, visited));

            return node;
        }

        private static bool IsDeclaredType(TypeDefinition type)
        {
            return type != null && !string.IsNullOrEmpty(type.TypeName) && type.TypeName != RootName;
        }
    }
}
