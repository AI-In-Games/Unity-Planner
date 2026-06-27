using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AIInGames.Planning.Unity
{
    /// <summary>
    /// Represents a PDDL predicate definition (e.g., "(at ?obj - object ?loc - location)").
    /// </summary>
    [Serializable]
    public class PredicateDefinition
    {
        [SerializeField] private string predicateName;
        [SerializeField] private List<PredicateParameter> parameters = new List<PredicateParameter>();

        public PredicateDefinition()
        {
            predicateName = "new-predicate";
        }

        public PredicateDefinition(string name)
        {
            predicateName = name;
        }

        public string PredicateName
        {
            get => predicateName;
            set => predicateName = value;
        }

        public List<PredicateParameter> Parameters => parameters;

        /// <summary>
        /// Returns PDDL representation: "(predicate-name ?param1 - type1 ?param2 - type2)"
        /// </summary>
        public string ToPddlString()
        {
            if (parameters.Count == 0)
            {
                return $"({predicateName})";
            }

            var paramStrings = parameters.Select(p => p.ToPddlString());
            return $"({predicateName} {string.Join(" ", paramStrings)})";
        }

        public override string ToString() => ToPddlString();
    }

    /// <summary>
    /// A single parameter in a predicate definition.
    /// </summary>
    [Serializable]
    public class PredicateParameter
    {
        [SerializeField] private string parameterName;
        [SerializeField] private string parameterType = "object";

        public PredicateParameter()
        {
            parameterName = "?param";
            parameterType = "object";
        }

        public PredicateParameter(string name, string type)
        {
            parameterName = name;
            parameterType = type;
        }

        public string ParameterName
        {
            get => parameterName;
            set => parameterName = value;
        }

        public string ParameterType
        {
            get => parameterType;
            set => parameterType = value;
        }

        public string ToPddlString()
        {
            return $"{parameterName} - {parameterType}";
        }

        public override string ToString() => ToPddlString();
    }
}
