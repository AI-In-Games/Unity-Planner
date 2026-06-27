using System;
using UnityEngine;

namespace AIInGames.Planning.Unity
{
    /// <summary>
    /// Represents a PDDL type definition (e.g., "robot - object", "location").
    /// </summary>
    [Serializable]
    public class TypeDefinition
    {
        [SerializeField] private string typeName;
        [SerializeField] private string parentType = "object"; // Default PDDL parent type

        public TypeDefinition()
        {
            typeName = "new-type";
            parentType = "object";
        }

        public TypeDefinition(string name, string parent = "object")
        {
            typeName = name;
            parentType = parent;
        }

        public string TypeName
        {
            get => typeName;
            set => typeName = value;
        }

        public string ParentType
        {
            get => parentType;
            set => parentType = value;
        }

        /// <summary>
        /// Returns PDDL representation: "typename - parenttype"
        /// </summary>
        public string ToPddlString()
        {
            if (string.IsNullOrEmpty(parentType) || parentType == "object")
            {
                return typeName;
            }
            return $"{typeName} - {parentType}";
        }

        public override string ToString() => ToPddlString();
    }
}
