using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace AIInGames.Planning.Runtime
{
    /// <summary>
    /// Serializes a PlanResult to a human-readable JSON string for debugging.
    /// </summary>
    public static class PlanSerializer
    {
        /// <summary>
        /// Returns a JSON string with the plan summary: success, cost, search stats, and action list.
        /// </summary>
        public static string ToJson(PlanResult result)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{\n");
            AppendHeader(sb, result);

            if (result.Success)
            {
                List<GroundedAction> actions = result.Actions;
                if (actions.Count == 0)
                {
                    sb.Append(",\n  \"steps\": []");
                }
                else
                {
                    sb.Append(",\n  \"steps\": [");
                    for (int i = 0; i < actions.Count; i++)
                    {
                        sb.Append("\n    { \"step\": ");
                        sb.Append(i + 1);
                        sb.Append(", \"action\": \"");
                        sb.Append(Escape(actions[i].Name));
                        sb.Append("\", \"cost\": ");
                        sb.Append(F(actions[i].Cost));
                        sb.Append(" }");
                        if (i < actions.Count - 1)
                            sb.Append(",");
                    }
                    sb.Append("\n  ]");
                }
            }

            sb.Append("\n}");
            return sb.ToString();
        }

        /// <summary>
        /// Returns a JSON string that includes the initial state predicates and shows
        /// which predicates each action adds or removes at each step.
        /// </summary>
        public static string ToJson(PlanResult result, PlanningState initialState)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{\n");
            AppendHeader(sb, result);
            AppendPredicateMap(sb, "initialState", initialState);

            if (result.Success)
            {
                List<GroundedAction> actions = result.Actions;
                if (actions.Count == 0)
                {
                    sb.Append(",\n  \"steps\": []");
                }
                else
                {
                    sb.Append(",\n  \"steps\": [");
                    for (int i = 0; i < actions.Count; i++)
                    {
                        GroundedAction action = actions[i];
                        sb.Append("\n    {\n");
                        sb.Append("      \"step\": "); sb.Append(i + 1); sb.Append(",\n");
                        sb.Append("      \"action\": \""); sb.Append(Escape(action.Name)); sb.Append("\",\n");
                        sb.Append("      \"cost\": "); sb.Append(F(action.Cost)); sb.Append(",\n");
                        AppendEffects(sb, action.Effects);
                        sb.Append("\n    }");
                        if (i < actions.Count - 1)
                            sb.Append(",");
                    }
                    sb.Append("\n  ]");
                }
            }

            sb.Append("\n}");
            return sb.ToString();
        }

        private static void AppendHeader(StringBuilder sb, PlanResult result)
        {
            sb.Append("  \"success\": "); sb.Append(result.Success ? "true" : "false");
            if (result.Success)
            {
                sb.Append(",\n  \"totalCost\": "); sb.Append(F(result.TotalCost));
            }
            sb.Append(",\n  \"nodesExpanded\": "); sb.Append(result.NodesExpanded);
            sb.Append(",\n  \"iterations\": "); sb.Append(result.Iterations);
        }

        private static void AppendPredicateMap(StringBuilder sb, string key, PlanningState state)
        {
            sb.Append(",\n  \""); sb.Append(key); sb.Append("\": {");
            bool first = true;
            foreach (KeyValuePair<string, bool> kvp in state.Predicates)
            {
                if (!first) sb.Append(",");
                sb.Append(" \""); sb.Append(Escape(kvp.Key)); sb.Append("\": ");
                sb.Append(kvp.Value ? "true" : "false");
                first = false;
            }
            sb.Append(" }");
        }

        private static void AppendEffects(StringBuilder sb, PlanningState effects)
        {
            sb.Append("      \"effects\": {");
            bool first = true;
            foreach (KeyValuePair<string, bool> kvp in effects.Predicates)
            {
                if (!first) sb.Append(",");
                sb.Append("\n        \"");
                sb.Append(Escape(kvp.Key));
                sb.Append("\": ");
                sb.Append(kvp.Value ? "true" : "false");
                first = false;
            }
            if (!first) sb.Append("\n      ");
            sb.Append("}");
        }

        private static string F(float value)
        {
            return value.ToString("G", CultureInfo.InvariantCulture);
        }

        private static string Escape(string value)
        {
            if (value == null) return string.Empty;

            StringBuilder sb = new StringBuilder(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                switch (c)
                {
                    case '"':  sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n");  break;
                    case '\r': sb.Append("\\r");  break;
                    case '\t': sb.Append("\\t");  break;
                    default:   sb.Append(c);      break;
                }
            }
            return sb.ToString();
        }
    }
}
