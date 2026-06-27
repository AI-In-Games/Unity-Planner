using System;
using System.Collections.Generic;
using UnityEngine;

namespace AIInGames.Planning.Unity
{
    /// <summary>
    /// Unity asset representing a planning domain. Domain data is stored as JSON,
    /// so the .asset file contains human-readable JSON content.
    ///
    /// Use DomainSerializer to convert between JSON, DomainData, and PDDL.
    /// </summary>
    [CreateAssetMenu(fileName = "NewDomain", menuName = "AI Planning/Domain", order = 1)]
    public class DomainAsset : ScriptableObject, ISerializationCallbackReceiver
    {
        [SerializeField, HideInInspector] private string m_Json = "{}";

        private string m_DomainName = "new-domain";
        private List<string> m_Requirements = new List<string> { ":strips", ":typing" };
        private List<TypeDefinition> m_Types = new List<TypeDefinition>();
        private List<PredicateDefinition> m_Predicates = new List<PredicateDefinition>();
        private List<ActionDefinition> m_Actions = new List<ActionDefinition>();

        public string DomainName
        {
            get => m_DomainName;
            set => m_DomainName = value;
        }

        public List<string> Requirements => m_Requirements;
        public List<TypeDefinition> Types => m_Types;
        public List<PredicateDefinition> Predicates => m_Predicates;
        public List<ActionDefinition> Actions => m_Actions;

        /// <summary>
        /// The raw JSON string that backs this asset on disk.
        /// Assign a JSON string produced by DomainSerializer.ToJson to replace all domain content.
        /// </summary>
        public string Json
        {
            get => m_Json;
            set
            {
                m_Json = value;
                HydrateFromJson();
            }
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            m_Json = DomainSerializer.ToJson(ToDomainData(), prettyPrint: true);
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            HydrateFromJson();
        }

        private void HydrateFromJson()
        {
            if (string.IsNullOrEmpty(m_Json) || m_Json == "{}")
                return;
            DomainData data = DomainSerializer.FromJson(m_Json);
            DomainSerializer.Apply(data, this);
        }

        private DomainData ToDomainData()
        {
            return DomainSerializer.ToDomainData(this);
        }
    }

    /// <summary>
    /// Stores visual graph layout data for the domain editor window.
    /// </summary>
    [Serializable]
    public class GraphData
    {
        [SerializeField] private List<NodeData> nodes = new List<NodeData>();
        [SerializeField] private List<EdgeData> edges = new List<EdgeData>();
        [SerializeField] private Vector2 viewPosition = Vector2.zero;
        [SerializeField] private Vector3 viewScale = Vector3.one;

        public List<NodeData> Nodes => nodes;
        public List<EdgeData> Edges => edges;
        public Vector2 ViewPosition { get => viewPosition; set => viewPosition = value; }
        public Vector3 ViewScale { get => viewScale; set => viewScale = value; }
    }

    [Serializable]
    public class NodeData
    {
        public string guid;
        public string nodeType;
        public Vector2 position;
        public string referenceId;
    }

    [Serializable]
    public class EdgeData
    {
        public string guid;
        public string outputNodeGuid;
        public string inputNodeGuid;
        public string outputPortName;
        public string inputPortName;
    }
}
