using AIInGames.Planning.Runtime;
using UnityEngine;

namespace AIInGames.Planning.Demo
{
    /// <summary>
    /// Shared IMGUI helpers for the demo overlays. Every scene renders the current plan the same way,
    /// with a marker for completed, running, and pending steps.
    /// </summary>
    public static class DemoHud
    {
        public static void DrawPlan(PlanResult plan, PlanExecutor executor)
        {
            GUILayout.Label("Current plan:");

            if (plan == null || plan.Actions.Count == 0)
            {
                GUILayout.Label("  (none)");
                return;
            }

            int current = executor != null ? executor.CurrentActionIndex : -1;
            bool executing = executor != null && executor.State == PlanExecutor.ExecutionState.Executing;
            for (int i = 0; i < plan.Actions.Count; i++)
            {
                string marker = i < current ? "[x]" : (i == current && executing ? "[>]" : "[ ]");
                GUILayout.Label($"  {marker} {plan.Actions[i].Name}");
            }
        }
    }
}
