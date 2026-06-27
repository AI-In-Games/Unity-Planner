using System;
using UnityEditor;
using UnityEngine;

namespace AIInGames.Planning.Unity.Editor
{
    /// <summary>
    /// Provides a disposable scope for Unity undo operations.
    /// Automatically calls Undo.RecordObject at the start and EditorUtility.SetDirty at the end.
    ///
    /// Usage:
    /// <code>
    /// using (new UndoScope(myObject, "Operation Name"))
    /// {
    ///     myObject.SomeProperty = newValue;
    /// }
    /// </code>
    /// </summary>
    public sealed class UndoScope : IDisposable
    {
        private readonly UnityEngine.Object _target;
        private bool _disposed;

        /// <summary>
        /// Creates a new undo scope for the specified target object.
        /// Immediately records the object's state for undo.
        /// </summary>
        /// <param name="target">The Unity object to track for undo</param>
        /// <param name="operationName">The name shown in Unity's Edit > Undo menu</param>
        public UndoScope(UnityEngine.Object target, string operationName)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            if (string.IsNullOrEmpty(operationName))
                throw new ArgumentException("Operation name cannot be null or empty", nameof(operationName));

            _target = target;
            Undo.RecordObject(_target, operationName);
        }

        /// <summary>
        /// Marks the target object as dirty, ensuring changes are saved.
        /// Called automatically when the using block exits.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            if (_target != null)
            {
                EditorUtility.SetDirty(_target);
            }

            _disposed = true;
        }
    }
}
