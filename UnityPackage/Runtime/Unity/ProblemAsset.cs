using System;
using System.Collections.Generic;
using UnityEngine;

namespace AIInGames.Planning.Unity
{
    /// <summary>
    /// ScriptableObject representation of a PDDL problem.
    /// References a domain and defines objects, initial state, and goal.
    /// </summary>
    [CreateAssetMenu(fileName = "NewProblem", menuName = "AI Planning/Problem", order = 2)]
    public class ProblemAsset : ScriptableObject
    {
        [SerializeField] private string problemName = "new-problem";
        [SerializeField] private DomainAsset domain;

        [Header("Problem Definition")]
        [SerializeField] private List<ObjectDefinition> objects = new List<ObjectDefinition>();
        [SerializeField] private List<ConditionNode> initialState = new List<ConditionNode>();
        [SerializeField] private List<ConditionNode> goalState = new List<ConditionNode>();

        [Header("PDDL Export")]
        [SerializeField, TextArea(10, 30)] private string cachedPddl = "";

        public string ProblemName
        {
            get => problemName;
            set => problemName = value;
        }

        public DomainAsset Domain
        {
            get => domain;
            set => domain = value;
        }

        public List<ObjectDefinition> Objects => objects;
        public List<ConditionNode> InitialState => initialState;
        public List<ConditionNode> GoalState => goalState;

        public string CachedPddl => cachedPddl;

        /// <summary>
        /// Assigns JSON produced by ProblemSerializer.ToJson to replace all problem content.
        /// The domain asset reference is preserved; only name, objects, and states are replaced.
        /// </summary>
        public string Json
        {
            get => ProblemSerializer.ToJson(ProblemSerializer.ToProblemData(this));
            set
            {
                ProblemData data = ProblemSerializer.FromJson(value);
                ProblemSerializer.Apply(data, this, domain);
            }
        }

        /// <summary>
        /// Regenerates the cached PDDL string from current problem data.
        /// </summary>
        public void RegeneratePddl()
        {
            cachedPddl = ProblemSerializer.ToPddl(ProblemSerializer.ToProblemData(this));
        }

        private void OnValidate()
        {
            RegeneratePddl();
        }
    }

    /// <summary>
    /// Represents an object instance in a PDDL problem.
    /// </summary>
    [Serializable]
    public class ObjectDefinition
    {
        [SerializeField] private string objectName;
        [SerializeField] private string objectType = "object";

        public ObjectDefinition()
        {
            objectName = "new-object";
            objectType = "object";
        }

        public ObjectDefinition(string name, string type)
        {
            objectName = name;
            objectType = type;
        }

        public string ObjectName
        {
            get => objectName;
            set => objectName = value;
        }

        public string ObjectType
        {
            get => objectType;
            set => objectType = value;
        }

        public string ToPddlString() => $"{objectName} - {objectType}";
        public override string ToString() => ToPddlString();
    }
}
