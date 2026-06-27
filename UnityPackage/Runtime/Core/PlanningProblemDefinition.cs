using System;
using System.Collections.Generic;

namespace AIInGames.Planning.Runtime
{
    /// <summary>
    /// Runtime-ready problem snapshot: objects, initial state, and goal state.
    /// </summary>
    public sealed class PlanningProblemDefinition
    {
        private readonly List<PlanningObject> m_Objects;

        public string Name { get; }
        public IReadOnlyList<PlanningObject> Objects => m_Objects;
        public PlanningState InitialState { get; }
        public PlanningState GoalState { get; }

        internal PlanningProblemDefinition(
            string name,
            List<PlanningObject> objects,
            PlanningState initialState,
            PlanningState goalState)
        {
            Name = RequireName(name, nameof(name));
            m_Objects = new List<PlanningObject>(objects ?? throw new ArgumentNullException(nameof(objects)));
            InitialState = initialState?.Clone() ?? throw new ArgumentNullException(nameof(initialState));
            GoalState = goalState?.Clone() ?? throw new ArgumentNullException(nameof(goalState));
        }

        public static PlanningProblemBuilder Builder(string name = "problem")
        {
            return new PlanningProblemBuilder(name);
        }

        public List<PlanningObject> CreateObjectList()
        {
            return new List<PlanningObject>(m_Objects);
        }

        public PlanningState CreateInitialState()
        {
            return InitialState.Clone();
        }

        public PlanningState CreateGoalState()
        {
            return GoalState.Clone();
        }

        public static string PredicateKey(string predicate, params string[] args)
        {
            return PlanningPredicate.BuildKey(predicate, args);
        }

        internal static string RequireName(string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Name cannot be null, empty, or whitespace.", paramName);
            return value.Trim();
        }
    }

    public sealed class PlanningProblemBuilder
    {
        private readonly List<PlanningObject> m_Objects = new List<PlanningObject>();
        private readonly Dictionary<string, PlanningObject> m_ObjectsByName =
            new Dictionary<string, PlanningObject>(StringComparer.Ordinal);
        private readonly PlanningState m_InitialState = new PlanningState();
        private readonly PlanningState m_GoalState = new PlanningState();
        private string m_Name;

        internal PlanningProblemBuilder(string name)
        {
            m_Name = PlanningProblemDefinition.RequireName(name, nameof(name));
        }

        public PlanningProblemBuilder Named(string name)
        {
            m_Name = PlanningProblemDefinition.RequireName(name, nameof(name));
            return this;
        }

        public PlanningProblemBuilder Object(string name, string type = "object")
        {
            name = PlanningProblemDefinition.RequireName(name, nameof(name));
            type = PlanningProblemDefinition.RequireName(type, nameof(type));

            if (m_ObjectsByName.ContainsKey(name))
                throw new ArgumentException($"Object '{name}' is already defined.", nameof(name));

            PlanningObject obj = new PlanningObject(name, type);
            m_Objects.Add(obj);
            m_ObjectsByName[name] = obj;
            return this;
        }

        public PlanningProblemBuilder Initially(string predicate, params string[] args)
        {
            return InitialPredicate(predicate, true, args);
        }

        public PlanningProblemBuilder InitiallyNot(string predicate, params string[] args)
        {
            return InitialPredicate(predicate, false, args);
        }

        public PlanningProblemBuilder Goal(string predicate, params string[] args)
        {
            return GoalPredicate(predicate, true, args);
        }

        public PlanningProblemBuilder GoalNot(string predicate, params string[] args)
        {
            return GoalPredicate(predicate, false, args);
        }

        public PlanningProblemBuilder InitialPredicate(string predicate, bool value, params string[] args)
        {
            m_InitialState.SetPredicate(predicate, value, args);
            return this;
        }

        public PlanningProblemBuilder GoalPredicate(string predicate, bool value, params string[] args)
        {
            m_GoalState.SetPredicate(predicate, value, args);
            return this;
        }

        public PlanningProblemDefinition Build()
        {
            return new PlanningProblemDefinition(m_Name, m_Objects, m_InitialState, m_GoalState);
        }
    }
}
