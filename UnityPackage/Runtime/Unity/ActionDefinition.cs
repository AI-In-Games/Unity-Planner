using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AIInGames.Planning.Unity
{
    /// <summary>
    /// Represents a PDDL action with parameters, preconditions, and effects.
    /// </summary>
    [Serializable]
    public class ActionDefinition
    {
        [SerializeField] private string actionName;
        [SerializeField] private List<ActionParameter> parameters = new List<ActionParameter>();
        [SerializeField] private ConditionGroup preconditions = new ConditionGroup();
        [SerializeField] private EffectGroup effects = new EffectGroup();

        public ActionDefinition()
        {
            actionName = "new-action";
        }

        public ActionDefinition(string name)
        {
            actionName = name;
        }

        public string ActionName
        {
            get => actionName;
            set => actionName = value;
        }

        public List<ActionParameter> Parameters => parameters;
        public ConditionGroup Preconditions => preconditions;
        public EffectGroup Effects => effects;

        /// <summary>
        /// Returns PDDL representation of the action.
        /// </summary>
        public string ToPddlString()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"(:action {actionName}");

            // Parameters
            if (parameters.Count > 0)
            {
                var paramStrings = parameters.Select(p => p.ToPddlString());
                sb.AppendLine($"  :parameters ({string.Join(" ", paramStrings)})");
            }

            // Preconditions
            if (preconditions.Conditions.Count > 0)
            {
                sb.AppendLine($"  :precondition {preconditions.ToPddlString()}");
            }

            // Effects
            if (effects.Effects.Count > 0)
            {
                sb.AppendLine($"  :effect {effects.ToPddlString()}");
            }

            sb.Append(")");
            return sb.ToString();
        }

        public override string ToString() => ToPddlString();
    }

    [Serializable]
    public class ActionParameter
    {
        [SerializeField] private string parameterName;
        [SerializeField] private string parameterType = "object";

        public ActionParameter()
        {
            parameterName = "?param";
            parameterType = "object";
        }

        public ActionParameter(string name, string type)
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

        public string ToPddlString() => $"{parameterName} - {parameterType}";
        public override string ToString() => ToPddlString();
    }

    /// <summary>
    /// Group of conditions (preconditions) with logical operators (AND/OR).
    /// Root container for the condition tree.
    /// </summary>
    [Serializable]
    public class ConditionGroup
    {
        public enum LogicalOperator { And, Or }

        [SerializeField] private LogicalOperator logicalOp = LogicalOperator.And;
        [SerializeField] private List<ConditionNode> conditions = new List<ConditionNode>();

        public LogicalOperator Operator
        {
            get => logicalOp;
            set => logicalOp = value;
        }

        public List<ConditionNode> Conditions => conditions;

        public string ToPddlString()
        {
            if (conditions.Count == 0) return "()";
            if (conditions.Count == 1) return conditions[0].ToPddlString();

            var op = logicalOp == LogicalOperator.And ? "and" : "or";
            var condStrings = conditions.Select(c => c.ToPddlString());
            return $"({op} {string.Join(" ", condStrings)})";
        }
    }

    /// <summary>
    /// A node in the condition tree - can be a predicate, AND, OR, or NOT group.
    /// Supports recursive nesting for complex logical formulas.
    /// </summary>
    [Serializable]
    public class ConditionNode
    {
        public enum NodeType { Predicate, And, Or, Not }

        [SerializeField] private NodeType nodeType = NodeType.Predicate;

        // For Predicate nodes
        [SerializeField] private string predicateName;
        [SerializeField] private List<string> arguments = new List<string>();

        // For Group nodes (And, Or, Not)
        [SerializeReference] private List<ConditionNode> children = new List<ConditionNode>();

        public NodeType Type
        {
            get => nodeType;
            set => nodeType = value;
        }

        public string PredicateName
        {
            get => predicateName;
            set => predicateName = value;
        }

        public List<string> Arguments
        {
            get => arguments;
            set => arguments = value;
        }

        public List<ConditionNode> Children
        {
            get => children;
            set => children = value;
        }

        public string ToPddlString()
        {
            switch (nodeType)
            {
                case NodeType.Predicate:
                    if (string.IsNullOrEmpty(predicateName)) return "()";
                    var pred = arguments.Count > 0
                        ? $"({predicateName} {string.Join(" ", arguments)})"
                        : $"({predicateName})";
                    return pred;

                case NodeType.And:
                    if (children.Count == 0) return "()";
                    if (children.Count == 1) return children[0].ToPddlString();
                    return $"(and {string.Join(" ", children.Select(c => c.ToPddlString()))})";

                case NodeType.Or:
                    if (children.Count == 0) return "()";
                    if (children.Count == 1) return children[0].ToPddlString();
                    return $"(or {string.Join(" ", children.Select(c => c.ToPddlString()))})";

                case NodeType.Not:
                    if (children.Count == 0) return "()";
                    return $"(not {children[0].ToPddlString()})";

                default:
                    return "()";
            }
        }

        public override string ToString() => ToPddlString();
    }


    /// <summary>
    /// Group of effects with logical operators.
    /// </summary>
    [Serializable]
    public class EffectGroup
    {
        [SerializeField] private List<Effect> effects = new List<Effect>();

        public List<Effect> Effects => effects;

        public string ToPddlString()
        {
            if (effects.Count == 0) return "()";
            if (effects.Count == 1) return effects[0].ToPddlString();

            var effectStrings = effects.Select(e => e.ToPddlString());
            return $"(and {string.Join(" ", effectStrings)})";
        }
    }

    /// <summary>
    /// A single effect that adds or removes a predicate.
    /// </summary>
    [Serializable]
    public class Effect
    {
        public enum EffectType { Add, Remove }

        [SerializeField] private EffectType effectType = EffectType.Add;
        [SerializeField] private string predicateName;
        [SerializeField] private List<string> arguments = new List<string>();

        public EffectType Type
        {
            get => effectType;
            set => effectType = value;
        }

        public string PredicateName
        {
            get => predicateName;
            set => predicateName = value;
        }

        public List<string> Arguments
        {
            get => arguments;
            set => arguments = value;
        }

        public string ToPddlString()
        {
            var pred = arguments.Count > 0
                ? $"({predicateName} {string.Join(" ", arguments)})"
                : $"({predicateName})";

            return effectType == EffectType.Remove ? $"(not {pred})" : pred;
        }

        public override string ToString() => ToPddlString();
    }
}
