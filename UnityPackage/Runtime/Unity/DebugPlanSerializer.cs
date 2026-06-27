using System.Collections.Generic;
using System.Globalization;
using System.Text;
using AIInGames.Planning.Runtime;

namespace AIInGames.Planning.Unity
{
    /// <summary>
    /// Produces richer debug JSON from a PlanResult: a summary block with timing and search stats,
    /// and a per-step breakdown with cumulative g-cost and effects.
    ///
    /// When the DEBUG_PLAN scripting define is set, also includes the full list of expanded
    /// search nodes collected by Planner.
    ///
    /// This class has no effect on planning performance. It is only called explicitly
    /// by code that needs debug output.
    /// </summary>
    public static class DebugPlanSerializer
    {
        /// <summary>
        /// Returns a debug JSON string for the given plan result, replaying the plan from
        /// initialState to derive per-step g-cost and effects.
        /// </summary>
        public static string ToJson(PlanResult result, PlanningState initialState)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{\n");
            AppendSummary(sb, result);

            if (result.Success)
                AppendSteps(sb, result, initialState);

#if DEBUG_PLAN
            AppendSearchNodes(sb, result.DebugSearchNodes);
#endif

            sb.Append("\n}");
            return sb.ToString();
        }

        private static void AppendSummary(StringBuilder sb, PlanResult result)
        {
            sb.Append("  \"summary\": {\n");
            sb.Append("    \"success\": "); sb.Append(result.Success ? "true" : "false"); sb.Append(",\n");
            if (result.Success)
            {
                sb.Append("    \"plan_length\": "); sb.Append(result.Actions.Count); sb.Append(",\n");
                sb.Append("    \"total_cost\": "); sb.Append(F(result.TotalCost)); sb.Append(",\n");
            }
            sb.Append("    \"nodes_expanded\": "); sb.Append(result.NodesExpanded); sb.Append(",\n");
            sb.Append("    \"iterations\": "); sb.Append(result.Iterations); sb.Append(",\n");
            sb.Append("    \"elapsed_ms\": "); sb.Append(result.ElapsedMs); sb.Append("\n");
            sb.Append("  }");
        }

        private static void AppendSteps(StringBuilder sb, PlanResult result, PlanningState initialState)
        {
            sb.Append(",\n  \"steps\": [");

            PlanningState current = initialState;
            float gCost = 0;
            List<GroundedAction> actions = result.Actions;

            for (int i = 0; i < actions.Count; i++)
            {
                GroundedAction action = actions[i];
                gCost += action.Cost;

                sb.Append("\n    {\n");
                sb.Append("      \"step\": ");    sb.Append(i + 1);              sb.Append(",\n");
                sb.Append("      \"action\": \""); sb.Append(Escape(action.Name)); sb.Append("\",\n");
                sb.Append("      \"cost\": ");    sb.Append(F(action.Cost));     sb.Append(",\n");
                sb.Append("      \"g_cost\": ");  sb.Append(F(gCost));           sb.Append(",\n");
                AppendEffects(sb, action.Effects);
                sb.Append("\n    }");

                if (i < actions.Count - 1)
                    sb.Append(",");

                current = action.Apply(current);
            }

            sb.Append("\n  ]");
        }

        private static void AppendEffects(StringBuilder sb, PlanningState effects)
        {
            sb.Append("      \"effects_added\": [");
            bool first = true;
            foreach (KeyValuePair<string, bool> kvp in effects.Predicates)
            {
                if (!kvp.Value) continue;
                if (!first) sb.Append(", ");
                sb.Append("\""); sb.Append(Escape(kvp.Key)); sb.Append("\"");
                first = false;
            }
            sb.Append("],\n");

            sb.Append("      \"effects_removed\": [");
            first = true;
            foreach (KeyValuePair<string, bool> kvp in effects.Predicates)
            {
                if (kvp.Value) continue;
                if (!first) sb.Append(", ");
                sb.Append("\""); sb.Append(Escape(kvp.Key)); sb.Append("\"");
                first = false;
            }
            sb.Append("]");
        }

#if DEBUG_PLAN
        private static void AppendSearchNodes(StringBuilder sb, List<SearchNodeDebugData> nodes)
        {
            sb.Append(",\n  \"search_nodes\": [");
            for (int i = 0; i < nodes.Count; i++)
            {
                SearchNodeDebugData n = nodes[i];
                sb.Append("\n    { \"action\": \""); sb.Append(Escape(n.ActionName));
                sb.Append("\", \"g\": "); sb.Append(F(n.GCost));
                sb.Append(", \"h\": ");  sb.Append(F(n.HCost));
                sb.Append(", \"f\": ");  sb.Append(F(n.FCost));
                sb.Append(" }");
                if (i < nodes.Count - 1) sb.Append(",");
            }
            sb.Append("\n  ]");
        }
#endif

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
