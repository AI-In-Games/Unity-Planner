using System.Collections.Generic;
using AIInGames.Planning.PDDL;
using AIInGames.Planning.Runtime;
using UnityEditor;
using UnityEngine;

namespace AIInGames.Planning.Unity.Editor
{
    /// <summary>
    /// Editor window for executing and visualizing plan results.
    /// Select a domain asset and a PDDL problem file, click Run Plan,
    /// then inspect each action step with its cost and effects.
    /// </summary>
    public class PlanVisualizerWindow : EditorWindow
    {
        private DomainAsset   m_Domain;
        private string        m_ProblemFilePath = "";
        private PlanRunResult m_RunResult;
        private bool          m_HasRun;
        private bool[]        m_StepFoldouts;
        private Vector2       m_Scroll;

        [MenuItem("Window/AI Planning/Plan Visualizer")]
        public static void Open()
        {
            PlanVisualizerWindow window = GetWindow<PlanVisualizerWindow>();
            window.titleContent = new GUIContent("Plan Visualizer");
            window.minSize = new Vector2(420, 400);
        }

        public static void OpenWithDomain(DomainAsset domain)
        {
            PlanVisualizerWindow window = GetWindow<PlanVisualizerWindow>();
            window.titleContent = new GUIContent("Plan Visualizer");
            window.minSize = new Vector2(420, 400);
            window.m_Domain = domain;
        }

        private void OnGUI()
        {
            DrawInputSection();

            EditorGUILayout.Space(6);

            bool canRun = m_Domain != null && !string.IsNullOrEmpty(m_ProblemFilePath);
            EditorGUI.BeginDisabledGroup(!canRun);
            if (GUILayout.Button("Run Plan", GUILayout.Height(28)))
                RunPlan();
            EditorGUI.EndDisabledGroup();

            if (!canRun)
            {
                string msg = m_Domain == null
                    ? "Assign a DomainAsset to enable planning."
                    : "Select a PDDL problem file to enable planning.";
                EditorGUILayout.HelpBox(msg, MessageType.Info);
                return;
            }

            if (m_HasRun)
            {
                EditorGUILayout.Space(4);
                DrawSeparator();
                EditorGUILayout.Space(4);
                DrawResultSection();
            }
        }

        private void DrawInputSection()
        {
            m_Domain = (DomainAsset)EditorGUILayout.ObjectField("Domain", m_Domain, typeof(DomainAsset), false);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Problem File", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(
                string.IsNullOrEmpty(m_ProblemFilePath) ? "(none)" : m_ProblemFilePath,
                EditorStyles.textField,
                GUILayout.ExpandWidth(true));
            if (GUILayout.Button("Browse...", GUILayout.Width(80)))
            {
                string path = EditorUtility.OpenFilePanel("Select PDDL Problem File", "", "pddl");
                if (!string.IsNullOrEmpty(path))
                {
                    m_ProblemFilePath = path;
                    m_HasRun          = false;
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawResultSection()
        {
            m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll);

            if (m_RunResult.HasError)
            {
                EditorGUILayout.HelpBox(m_RunResult.ErrorMessage, MessageType.Error);
                EditorGUILayout.EndScrollView();
                return;
            }

            if (m_RunResult.PlanResult == null)
            {
                EditorGUILayout.EndScrollView();
                return;
            }

            DrawSummary();

            if (m_RunResult.PlanResult.Success)
            {
                EditorGUILayout.Space(4);
                DrawSteps();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawSummary()
        {
            PlanResult result = m_RunResult.PlanResult;
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUIStyle statusStyle = new GUIStyle(EditorStyles.boldLabel);
                statusStyle.fontSize = 13;
                statusStyle.normal.textColor = result.Success
                    ? new Color(0.2f, 0.85f, 0.2f)
                    : new Color(0.9f, 0.25f, 0.25f);
                EditorGUILayout.LabelField(result.Success ? "PLAN FOUND" : "NO PLAN FOUND", statusStyle);

                if (result.Success)
                    EditorGUILayout.LabelField($"Steps: {result.Actions.Count}     Total cost: {result.TotalCost:G}     Elapsed: {result.ElapsedMs} ms");

                EditorGUILayout.LabelField($"Nodes expanded: {result.NodesExpanded}     Iterations: {result.Iterations}");
            }
        }

        private void DrawSteps()
        {
            List<GroundedAction> actions = m_RunResult.PlanResult.Actions;

            if (m_StepFoldouts == null || m_StepFoldouts.Length != actions.Count)
                m_StepFoldouts = new bool[actions.Count];

            GUIStyle addStyle    = new GUIStyle(EditorStyles.label);
            GUIStyle removeStyle = new GUIStyle(EditorStyles.label);
            addStyle.normal.textColor    = new Color(0.2f, 0.8f, 0.2f);
            removeStyle.normal.textColor = new Color(0.9f, 0.3f, 0.3f);

            float gCost = 0f;
            for (int i = 0; i < actions.Count; i++)
            {
                GroundedAction action = actions[i];
                gCost += action.Cost;

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    string header = $"Step {i + 1}:  {action.Name}     cost {action.Cost:G}     g = {gCost:G}";
                    m_StepFoldouts[i] = EditorGUILayout.Foldout(m_StepFoldouts[i], header, true);

                    if (m_StepFoldouts[i])
                    {
                        bool hasEffects = false;
                        foreach (System.Collections.Generic.KeyValuePair<string, bool> kvp in action.Effects.Predicates)
                        {
                            hasEffects = true;
                            GUIStyle style  = kvp.Value ? addStyle : removeStyle;
                            string   prefix = kvp.Value ? "+" : "-";
                            EditorGUILayout.LabelField($"  {prefix}  {kvp.Key}", style);
                        }

                        if (!hasEffects)
                            EditorGUILayout.LabelField("  (no effects)", EditorStyles.miniLabel);
                    }
                }
            }
        }

        private void RunPlan()
        {
            m_HasRun       = true;
            m_StepFoldouts = null;

            IParseResult<IProblem> parseResult = new PDDLParser().ParseProblemFile(m_ProblemFilePath);
            if (!parseResult.Success)
            {
                string msg = parseResult.Errors.Count > 0
                    ? parseResult.Errors[0].Message
                    : "Failed to parse problem file.";
                m_RunResult = PlanRunResult.Failure(msg);
                return;
            }

            IProblem problem = parseResult.Result;
            List<PlanningObject> objects = ProblemConverter.ToObjects(problem);
            PlanningState initial        = ProblemConverter.ToInitialState(problem);

            if (!ProblemConverter.TryConvertGoal(problem.Goal, out PlanningState goal, out string goalError))
            {
                m_RunResult = PlanRunResult.Failure(goalError);
                return;
            }

            PlanRunner runner = new PlanRunner { Domain = m_Domain };
            m_RunResult = runner.Run(objects, initial, goal);
        }

        private static void DrawSeparator()
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 1f);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.4f));
        }
    }
}
