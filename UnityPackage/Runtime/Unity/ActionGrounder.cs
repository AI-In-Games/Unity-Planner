using System;
using System.Collections.Generic;
using AIInGames.Planning.Unity;

namespace AIInGames.Planning.Runtime
{
    public class ActionGrounder
    {
        private readonly List<PlanningObject> m_Objects;
        private readonly Dictionary<string, List<PlanningObject>> m_ObjectsByType;

        public ActionGrounder(List<PlanningObject> objects)
        {
            m_Objects = objects;
            m_ObjectsByType = new Dictionary<string, List<PlanningObject>>(StringComparer.Ordinal);

            foreach (PlanningObject obj in objects)
            {
                if (!m_ObjectsByType.ContainsKey(obj.Type))
                {
                    m_ObjectsByType[obj.Type] = new List<PlanningObject>();
                }
                m_ObjectsByType[obj.Type].Add(obj);
            }
        }

        public List<GroundedAction> GroundAction(ActionDefinition actionDef, float cost = 1.0f, bool filterSelfLoops = true)
        {
            List<GroundedAction> groundedActions = new List<GroundedAction>();
            List<Dictionary<string, string>> bindings = GenerateBindings(actionDef.Parameters);

            // The parameter names are identical for every grounding of this schema, so build the array
            // once and share the reference across all of them; only the bound values differ per binding.
            string[] parameterNames = BuildParameterNames(actionDef.Parameters);

            foreach (Dictionary<string, string> binding in bindings)
            {
                if (filterSelfLoops && HasSelfLoop(actionDef.Parameters, binding))
                    continue;

                string[] arguments = BuildArguments(actionDef.Parameters, binding);
                PlanningState preconditions = BuildPreconditions(actionDef.Preconditions, binding);
                PlanningState effects = BuildEffects(actionDef.Effects, binding);
                groundedActions.Add(new GroundedAction(actionDef.ActionName, parameterNames, arguments, cost, preconditions, effects));
            }

            return groundedActions;
        }

        public List<GroundedAction> GroundAllActions(List<ActionDefinition> actionDefs, float defaultCost = 1.0f, bool filterSelfLoops = true)
        {
            List<GroundedAction> allGroundedActions = new List<GroundedAction>();

            foreach (ActionDefinition actionDef in actionDefs)
            {
                List<GroundedAction> grounded = GroundAction(actionDef, defaultCost, filterSelfLoops);
                allGroundedActions.AddRange(grounded);
            }

            return allGroundedActions;
        }

        private bool HasSelfLoop(List<ActionParameter> parameters, Dictionary<string, string> binding)
        {
            for (int i = 0; i < parameters.Count; i++)
            {
                for (int j = i + 1; j < parameters.Count; j++)
                {
                    if (parameters[i].ParameterType != parameters[j].ParameterType)
                        continue;

                    if (binding[parameters[i].ParameterName] == binding[parameters[j].ParameterName])
                        return true;
                }
            }
            return false;
        }

        private List<Dictionary<string, string>> GenerateBindings(List<ActionParameter> parameters)
        {
            if (parameters.Count == 0)
            {
                return new List<Dictionary<string, string>> { new Dictionary<string, string>(StringComparer.Ordinal) };
            }

            List<Dictionary<string, string>> bindings = new List<Dictionary<string, string>>();
            GenerateBindingsRecursive(parameters, 0, new Dictionary<string, string>(StringComparer.Ordinal), bindings);
            return bindings;
        }

        private void GenerateBindingsRecursive(List<ActionParameter> parameters, int index,
            Dictionary<string, string> currentBinding, List<Dictionary<string, string>> allBindings)
        {
            if (index >= parameters.Count)
            {
                allBindings.Add(new Dictionary<string, string>(currentBinding, StringComparer.Ordinal));
                return;
            }

            ActionParameter param = parameters[index];
            string paramType = param.ParameterType;

            if (!m_ObjectsByType.ContainsKey(paramType))
            {
                return;
            }

            foreach (PlanningObject obj in m_ObjectsByType[paramType])
            {
                currentBinding[param.ParameterName] = obj.Name;
                GenerateBindingsRecursive(parameters, index + 1, currentBinding, allBindings);
                currentBinding.Remove(param.ParameterName);
            }
        }

        private static string[] BuildParameterNames(List<ActionParameter> parameters)
        {
            string[] parameterNames = new string[parameters.Count];
            for (int i = 0; i < parameters.Count; i++)
                parameterNames[i] = parameters[i].ParameterName;
            return parameterNames;
        }

        private static string[] BuildArguments(List<ActionParameter> parameters, Dictionary<string, string> binding)
        {
            string[] arguments = new string[parameters.Count];
            for (int i = 0; i < parameters.Count; i++)
            {
                arguments[i] = binding.TryGetValue(parameters[i].ParameterName, out string value)
                    ? value
                    : parameters[i].ParameterName;
            }
            return arguments;
        }

        private PlanningState BuildPreconditions(ConditionGroup conditionGroup, Dictionary<string, string> binding)
        {
            PlanningState state = new PlanningState();

            foreach (ConditionNode condition in conditionGroup.Conditions)
            {
                ProcessConditionNode(condition, binding, state);
            }

            return state;
        }

        private void ProcessConditionNode(ConditionNode node, Dictionary<string, string> binding, PlanningState state)
        {
            switch (node.Type)
            {
                case ConditionNode.NodeType.Predicate:
                    state.SetPredicate(node.PredicateName, true, SubstituteArguments(node.Arguments, binding));
                    break;

                case ConditionNode.NodeType.Not:
                    if (node.Children.Count > 0 && node.Children[0].Type == ConditionNode.NodeType.Predicate)
                    {
                        ConditionNode literal = node.Children[0];
                        state.SetPredicate(literal.PredicateName, false, SubstituteArguments(literal.Arguments, binding));
                    }
                    break;

                case ConditionNode.NodeType.And:
                    foreach (ConditionNode child in node.Children)
                    {
                        ProcessConditionNode(child, binding, state);
                    }
                    break;

                case ConditionNode.NodeType.Or:
                    break;
            }
        }

        private PlanningState BuildEffects(EffectGroup effectGroup, Dictionary<string, string> binding)
        {
            PlanningState state = new PlanningState();

            foreach (Effect effect in effectGroup.Effects)
            {
                bool value = effect.Type == Effect.EffectType.Add;
                state.SetPredicate(effect.PredicateName, value, SubstituteArguments(effect.Arguments, binding));
            }

            return state;
        }

        private string[] SubstituteArguments(List<string> arguments, Dictionary<string, string> binding)
        {
            if (arguments.Count == 0)
                return Array.Empty<string>();

            string[] boundArgs = new string[arguments.Count];
            for (int i = 0; i < arguments.Count; i++)
            {
                string arg = arguments[i];
                boundArgs[i] = binding.TryGetValue(arg, out string value) ? value : arg;
            }
            return boundArgs;
        }

    }
}
