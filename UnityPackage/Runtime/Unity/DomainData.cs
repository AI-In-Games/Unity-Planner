using System;
using System.Collections.Generic;

namespace AIInGames.Planning.Unity
{
    /// <summary>
    /// Plain data model for a planning domain. Serializes cleanly to JSON.
    /// </summary>
    [Serializable]
    public class DomainData
    {
        public string name = "new-domain";
        public List<string> requirements = new List<string>();
        public List<DomainTypeData> types = new List<DomainTypeData>();
        public List<DomainPredicateData> predicates = new List<DomainPredicateData>();
        public List<DomainActionData> actions = new List<DomainActionData>();
    }

    [Serializable]
    public class DomainTypeData
    {
        public string name;
        public string parent = "object";
    }

    [Serializable]
    public class DomainPredicateData
    {
        public string name;
        public List<DomainParameterData> parameters = new List<DomainParameterData>();
    }

    [Serializable]
    public class DomainActionData
    {
        public string name;
        public List<DomainParameterData> parameters = new List<DomainParameterData>();
        public DomainConditionData precondition = new DomainConditionData();
        public List<DomainEffectData> effects = new List<DomainEffectData>();
    }

    [Serializable]
    public class DomainParameterData
    {
        public string name;
        public string type;
    }

    /// <summary>
    /// Flat condition list for an action's preconditions. Non-recursive so JsonUtility
    /// does not hit the serialization depth limit.
    /// </summary>
    [Serializable]
    public class DomainConditionData
    {
        /// <summary>"and" or "or"</summary>
        public string op = "and";
        public List<DomainPredicateConditionData> conditions = new List<DomainPredicateConditionData>();
    }

    /// <summary>
    /// A single predicate in a condition list, optionally negated.
    /// </summary>
    [Serializable]
    public class DomainPredicateConditionData
    {
        public bool negated;
        public string predicate;
        public List<string> args = new List<string>();
    }

    [Serializable]
    public class DomainEffectData
    {
        /// <summary>true means (not predicate) — a delete effect.</summary>
        public bool negative;
        public string predicate;
        public List<string> args = new List<string>();
    }
}
