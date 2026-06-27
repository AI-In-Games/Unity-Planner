using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace AIInGames.Planning.Unity
{
    /// <summary>
    /// Converts between DomainData (the JSON-backed data model), DomainAsset (the Unity asset),
    /// and PDDL text.
    ///
    /// Conversion directions supported:
    ///   DomainAsset   → DomainData    (ToDomainData)
    ///   DomainData    → DomainAsset   (Apply)
    ///   DomainData   ↔  JSON string   (ToJson / FromJson)
    ///   DomainData    → PDDL string   (ToPddl)
    /// </summary>
    public static class DomainSerializer
    {
        // -------------------------------------------------------------------------
        // JSON
        // -------------------------------------------------------------------------

        /// <summary>
        /// Serializes a DomainData object to a JSON string.
        /// </summary>
        public static string ToJson(DomainData domain, bool prettyPrint = true)
        {
            return JsonUtility.ToJson(domain, prettyPrint);
        }

        /// <summary>
        /// Deserializes a DomainData object from a JSON string produced by ToJson.
        /// Returns a default DomainData when the input is null or empty.
        /// </summary>
        public static DomainData FromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                return new DomainData();
            DomainData result = JsonUtility.FromJson<DomainData>(json);
            return result ?? new DomainData();
        }

        // -------------------------------------------------------------------------
        // DomainAsset <-> DomainData
        // -------------------------------------------------------------------------

        /// <summary>
        /// Converts the current state of a DomainAsset into a DomainData object.
        /// </summary>
        public static DomainData ToDomainData(DomainAsset asset)
        {
            DomainData data = new DomainData();
            data.name = asset.DomainName;
            data.requirements = new List<string>(asset.Requirements);

            for (int i = 0; i < asset.Types.Count; i++)
            {
                TypeDefinition src = asset.Types[i];
                data.types.Add(new DomainTypeData { name = src.TypeName, parent = src.ParentType });
            }

            for (int i = 0; i < asset.Predicates.Count; i++)
            {
                PredicateDefinition src = asset.Predicates[i];
                DomainPredicateData pred = new DomainPredicateData { name = src.PredicateName };
                for (int j = 0; j < src.Parameters.Count; j++)
                {
                    PredicateParameter p = src.Parameters[j];
                    pred.parameters.Add(new DomainParameterData { name = p.ParameterName, type = p.ParameterType });
                }
                data.predicates.Add(pred);
            }

            for (int i = 0; i < asset.Actions.Count; i++)
            {
                ActionDefinition src = asset.Actions[i];
                DomainActionData action = new DomainActionData { name = src.ActionName };
                for (int j = 0; j < src.Parameters.Count; j++)
                {
                    ActionParameter p = src.Parameters[j];
                    action.parameters.Add(new DomainParameterData { name = p.ParameterName, type = p.ParameterType });
                }
                action.precondition = ConvertConditionGroup(src.Preconditions);
                for (int j = 0; j < src.Effects.Effects.Count; j++)
                {
                    Effect e = src.Effects.Effects[j];
                    action.effects.Add(new DomainEffectData
                    {
                        negative  = e.Type == Effect.EffectType.Remove,
                        predicate = e.PredicateName,
                        args      = new List<string>(e.Arguments)
                    });
                }
                data.actions.Add(action);
            }

            return data;
        }

        /// <summary>
        /// Populates a DomainAsset from the given DomainData, replacing all existing content.
        /// </summary>
        public static void Apply(DomainData data, DomainAsset asset)
        {
            asset.DomainName = data.name;
            asset.Requirements.Clear();
            asset.Requirements.AddRange(data.requirements);

            asset.Types.Clear();
            for (int i = 0; i < data.types.Count; i++)
            {
                DomainTypeData t = data.types[i];
                asset.Types.Add(new TypeDefinition(t.name, t.parent));
            }

            asset.Predicates.Clear();
            for (int i = 0; i < data.predicates.Count; i++)
            {
                DomainPredicateData dp = data.predicates[i];
                PredicateDefinition pred = new PredicateDefinition(dp.name);
                if (dp.parameters != null)
                {
                    for (int j = 0; j < dp.parameters.Count; j++)
                        pred.Parameters.Add(new PredicateParameter(dp.parameters[j].name, dp.parameters[j].type));
                }
                asset.Predicates.Add(pred);
            }

            asset.Actions.Clear();
            for (int i = 0; i < data.actions.Count; i++)
            {
                DomainActionData da = data.actions[i];
                ActionDefinition action = new ActionDefinition(da.name);
                if (da.parameters != null)
                {
                    for (int j = 0; j < da.parameters.Count; j++)
                        action.Parameters.Add(new ActionParameter(da.parameters[j].name, da.parameters[j].type));
                }
                if (da.precondition != null)
                    ApplyCondition(da.precondition, action.Preconditions);
                if (da.effects != null)
                {
                    for (int j = 0; j < da.effects.Count; j++)
                    {
                        DomainEffectData de = da.effects[j];
                        action.Effects.Effects.Add(new Effect
                        {
                            Type          = de.negative ? Effect.EffectType.Remove : Effect.EffectType.Add,
                            PredicateName = de.predicate,
                            Arguments     = new List<string>(de.args ?? new List<string>())
                        });
                    }
                }
                asset.Actions.Add(action);
            }
        }

        // -------------------------------------------------------------------------
        // PDDL generation
        // -------------------------------------------------------------------------

        /// <summary>
        /// Generates a PDDL domain string from the given DomainData.
        /// </summary>
        public static string ToPddl(DomainData data)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("(define (domain "); sb.Append(data.name); sb.AppendLine(")");

            if (data.requirements != null && data.requirements.Count > 0)
            {
                sb.Append("  (:requirements");
                for (int i = 0; i < data.requirements.Count; i++)
                {
                    sb.Append(" ");
                    sb.Append(data.requirements[i]);
                }
                sb.AppendLine(")");
            }

            if (data.types != null && data.types.Count > 0)
            {
                sb.AppendLine("  (:types");
                for (int i = 0; i < data.types.Count; i++)
                {
                    DomainTypeData t = data.types[i];
                    sb.Append("    "); sb.Append(t.name);
                    if (!string.IsNullOrEmpty(t.parent) && t.parent != "object")
                    {
                        sb.Append(" - "); sb.Append(t.parent);
                    }
                    sb.AppendLine();
                }
                sb.AppendLine("  )");
            }

            if (data.predicates != null && data.predicates.Count > 0)
            {
                sb.AppendLine("  (:predicates");
                for (int i = 0; i < data.predicates.Count; i++)
                {
                    DomainPredicateData pred = data.predicates[i];
                    sb.Append("    ("); sb.Append(pred.name);
                    if (pred.parameters != null)
                    {
                        for (int j = 0; j < pred.parameters.Count; j++)
                        {
                            sb.Append(" "); sb.Append(pred.parameters[j].name);
                            sb.Append(" - "); sb.Append(pred.parameters[j].type);
                        }
                    }
                    sb.AppendLine(")");
                }
                sb.AppendLine("  )");
            }

            if (data.actions != null)
            {
                for (int i = 0; i < data.actions.Count; i++)
                {
                    AppendAction(sb, data.actions[i]);
                }
            }

            sb.Append(")");
            return sb.ToString();
        }

        /// <summary>
        /// Generates a PDDL domain string directly from a DomainAsset.
        /// </summary>
        public static string ToPddl(DomainAsset asset)
        {
            return ToPddl(ToDomainData(asset));
        }

        // -------------------------------------------------------------------------
        // Private helpers
        // -------------------------------------------------------------------------

        private static DomainConditionData ConvertConditionGroup(ConditionGroup group)
        {
            DomainConditionData data = new DomainConditionData
            {
                op = group.Operator == ConditionGroup.LogicalOperator.Or ? "or" : "and"
            };

            for (int i = 0; i < group.Conditions.Count; i++)
                CollectPredicateConditions(group.Conditions[i], data.conditions);

            return data;
        }

        private static void CollectPredicateConditions(ConditionNode node, List<DomainPredicateConditionData> result)
        {
            switch (node.Type)
            {
                case ConditionNode.NodeType.Predicate:
                    result.Add(new DomainPredicateConditionData
                    {
                        negated   = false,
                        predicate = node.PredicateName,
                        args      = new List<string>(node.Arguments)
                    });
                    break;
                case ConditionNode.NodeType.Not:
                    if (node.Children.Count > 0 && node.Children[0].Type == ConditionNode.NodeType.Predicate)
                    {
                        result.Add(new DomainPredicateConditionData
                        {
                            negated   = true,
                            predicate = node.Children[0].PredicateName,
                            args      = new List<string>(node.Children[0].Arguments)
                        });
                    }
                    break;
                case ConditionNode.NodeType.And:
                case ConditionNode.NodeType.Or:
                    for (int i = 0; i < node.Children.Count; i++)
                        CollectPredicateConditions(node.Children[i], result);
                    break;
            }
        }

        private static void ApplyCondition(DomainConditionData source, ConditionGroup target)
        {
            target.Operator = source.op == "or"
                ? ConditionGroup.LogicalOperator.Or
                : ConditionGroup.LogicalOperator.And;

            if (source.conditions == null)
                return;

            for (int i = 0; i < source.conditions.Count; i++)
            {
                DomainPredicateConditionData pc = source.conditions[i];
                ConditionNode node = new ConditionNode();
                if (pc.negated)
                {
                    node.Type = ConditionNode.NodeType.Not;
                    ConditionNode predNode = new ConditionNode();
                    predNode.Type          = ConditionNode.NodeType.Predicate;
                    predNode.PredicateName = pc.predicate;
                    predNode.Arguments     = new List<string>(pc.args ?? new List<string>());
                    node.Children.Add(predNode);
                }
                else
                {
                    node.Type          = ConditionNode.NodeType.Predicate;
                    node.PredicateName = pc.predicate;
                    node.Arguments     = new List<string>(pc.args ?? new List<string>());
                }
                target.Conditions.Add(node);
            }
        }

        private static void AppendAction(StringBuilder sb, DomainActionData action)
        {
            sb.Append("  (:action "); sb.AppendLine(action.name);
            sb.Append("    :parameters (");
            if (action.parameters != null)
            {
                for (int i = 0; i < action.parameters.Count; i++)
                {
                    if (i > 0) sb.Append(" ");
                    sb.Append(action.parameters[i].name);
                    sb.Append(" - ");
                    sb.Append(action.parameters[i].type);
                }
            }
            sb.AppendLine(")");

            sb.Append("    :precondition ");
            AppendCondition(sb, action.precondition);
            sb.AppendLine();

            sb.Append("    :effect ");
            if (action.effects == null || action.effects.Count == 0)
            {
                sb.Append("()");
            }
            else if (action.effects.Count == 1)
            {
                AppendEffect(sb, action.effects[0]);
            }
            else
            {
                sb.Append("(and");
                for (int i = 0; i < action.effects.Count; i++)
                {
                    sb.Append(" ");
                    AppendEffect(sb, action.effects[i]);
                }
                sb.Append(")");
            }
            sb.AppendLine();
            sb.AppendLine("  )");
        }

        private static void AppendCondition(StringBuilder sb, DomainConditionData cond)
        {
            if (cond == null || cond.conditions == null || cond.conditions.Count == 0) { sb.Append("()"); return; }

            if (cond.conditions.Count == 1)
            {
                AppendPredicateCondition(sb, cond.conditions[0]);
                return;
            }

            sb.Append(cond.op == "or" ? "(or" : "(and");
            for (int i = 0; i < cond.conditions.Count; i++)
            {
                sb.Append(" ");
                AppendPredicateCondition(sb, cond.conditions[i]);
            }
            sb.Append(")");
        }

        private static void AppendPredicateCondition(StringBuilder sb, DomainPredicateConditionData pc)
        {
            if (pc.negated) sb.Append("(not ");
            sb.Append("("); sb.Append(pc.predicate);
            if (pc.args != null)
                for (int i = 0; i < pc.args.Count; i++) { sb.Append(" "); sb.Append(pc.args[i]); }
            sb.Append(")");
            if (pc.negated) sb.Append(")");
        }

        private static void AppendEffect(StringBuilder sb, DomainEffectData eff)
        {
            if (eff == null) { sb.Append("()"); return; }
            if (eff.negative) sb.Append("(not ");
            sb.Append("("); sb.Append(eff.predicate);
            if (eff.args != null)
                for (int i = 0; i < eff.args.Count; i++) { sb.Append(" "); sb.Append(eff.args[i]); }
            sb.Append(")");
            if (eff.negative) sb.Append(")");
        }
    }
}
