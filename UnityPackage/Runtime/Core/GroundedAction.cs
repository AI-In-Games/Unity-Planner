using System;
using System.Collections.Generic;

namespace AIInGames.Planning.Runtime
{
    public class GroundedAction
    {
        private static readonly string[] s_NoArguments = Array.Empty<string>();

        private readonly string[] m_ParameterNames;
        private readonly string[] m_Arguments;

        public string Name { get; }
        public string ActionName { get; }
        public float Cost { get; }
        public PlanningState Preconditions { get; }
        public PlanningState Effects { get; }

        /// <summary>The bound parameter values, in the action's parameter order.</summary>
        public IReadOnlyList<string> Arguments => m_Arguments;

        public GroundedAction(string name, float cost, PlanningState preconditions, PlanningState effects)
        {
            Name = name;
            ActionName = name;
            Cost = cost;
            Preconditions = preconditions;
            Effects = effects;
            m_ParameterNames = s_NoArguments;
            m_Arguments = s_NoArguments;
        }

        public GroundedAction(string actionName, string[] parameterNames, string[] arguments,
            float cost, PlanningState preconditions, PlanningState effects)
        {
            ActionName = actionName;
            m_ParameterNames = parameterNames ?? s_NoArguments;
            m_Arguments = arguments ?? s_NoArguments;
            Name = BuildName(actionName, m_Arguments);
            Cost = cost;
            Preconditions = preconditions;
            Effects = effects;
        }

        /// <summary>The value bound to a parameter, by the schema parameter name (for example "?to").</summary>
        public string Argument(string parameterName)
        {
            for (int i = 0; i < m_ParameterNames.Length; i++)
            {
                if (m_ParameterNames[i] == parameterName)
                    return m_Arguments[i];
            }
            throw new ArgumentException($"Action '{ActionName}' has no parameter '{parameterName}'.", nameof(parameterName));
        }

        public bool IsApplicable(PlanningState state)
        {
            return state.Satisfies(Preconditions);
        }

        public PlanningState Apply(PlanningState state)
        {
            PlanningState newState = state.Clone();
            newState.ApplyEffects(Effects);
            return newState;
        }

        public override string ToString()
        {
            return $"{Name} (cost: {Cost})";
        }

        private static string BuildName(string actionName, string[] arguments)
        {
            if (arguments.Length == 0)
                return actionName;
            return $"{actionName}({string.Join(", ", arguments)})";
        }
    }
}
