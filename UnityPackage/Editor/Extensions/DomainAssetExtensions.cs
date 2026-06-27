using AIInGames.Planning.Unity.Editor;

namespace AIInGames.Planning.Unity.Editor.Extensions
{
    /// <summary>
    /// Extension methods for DomainAsset that provide editor operations with automatic undo support.
    /// All operations use UndoScope internally to ensure proper undo/redo functionality.
    /// </summary>
    public static class DomainAssetExtensions
    {
        #region Type Operations

        /// <summary>
        /// Adds a type to the domain with undo support.
        /// </summary>
        public static void AddType(this DomainAsset domain, TypeDefinition type)
        {
            using (new UndoScope(domain, $"Add Type '{type.TypeName}'"))
            {
                domain.Types.Add(type);
            }
        }

        /// <summary>
        /// Removes a type from the domain with undo support.
        /// </summary>
        public static void RemoveType(this DomainAsset domain, TypeDefinition type)
        {
            using (new UndoScope(domain, $"Delete Type '{type.TypeName}'"))
            {
                domain.Types.Remove(type);
            }
        }

        /// <summary>
        /// Changes a type's parent with undo support.
        /// </summary>
        public static void ChangeTypeParent(this DomainAsset domain, TypeDefinition type, string newParent)
        {
            using (new UndoScope(domain, $"Change '{type.TypeName}' Parent"))
            {
                type.ParentType = newParent;
            }
        }

        #endregion

        #region Predicate Operations

        /// <summary>
        /// Adds a predicate to the domain with undo support.
        /// </summary>
        public static void AddPredicate(this DomainAsset domain, PredicateDefinition predicate)
        {
            using (new UndoScope(domain, $"Add Predicate '{predicate.PredicateName}'"))
            {
                domain.Predicates.Add(predicate);
            }
        }

        /// <summary>
        /// Removes a predicate from the domain with undo support.
        /// </summary>
        public static void RemovePredicate(this DomainAsset domain, PredicateDefinition predicate)
        {
            using (new UndoScope(domain, $"Delete Predicate '{predicate.PredicateName}'"))
            {
                domain.Predicates.Remove(predicate);
            }
        }

        #endregion

        #region Action Operations

        /// <summary>
        /// Adds an action to the domain with undo support.
        /// </summary>
        public static void AddAction(this DomainAsset domain, ActionDefinition action)
        {
            using (new UndoScope(domain, $"Add Action '{action.ActionName}'"))
            {
                domain.Actions.Add(action);
            }
        }

        /// <summary>
        /// Removes an action from the domain with undo support.
        /// </summary>
        public static void RemoveAction(this DomainAsset domain, ActionDefinition action)
        {
            using (new UndoScope(domain, $"Delete Action '{action.ActionName}'"))
            {
                domain.Actions.Remove(action);
            }
        }

        #endregion

        #region Complex Operations

        /// <summary>
        /// Begins a complex edit operation that may involve multiple changes.
        /// Use this for operations that need to group multiple changes into a single undo step.
        ///
        /// <code>
        /// using (domain.BeginEdit("Reorganize Types"))
        /// {
        ///     // Multiple operations here
        ///     type1.ParentType = "object";
        ///     type2.ParentType = "object";
        ///     domain.Types.RemoveAll(t => t.Obsolete);
        /// }
        /// </code>
        /// </summary>
        public static UndoScope BeginEdit(this DomainAsset domain, string operationName)
        {
            return new UndoScope(domain, operationName);
        }

        #endregion
    }
}
