using System;
using System.Collections.Generic;

namespace AIInGames.Planning.Unity
{
    /// <summary>
    /// Plain data model for a planning problem. Serializes cleanly to JSON.
    /// </summary>
    [Serializable]
    public class ProblemData
    {
        public string name = "new-problem";
        public string domain = "new-domain";
        public List<ProblemObjectData> objects = new List<ProblemObjectData>();
        public List<ProblemPredicateData> initial = new List<ProblemPredicateData>();
        public List<ProblemPredicateData> goal = new List<ProblemPredicateData>();
    }

    [Serializable]
    public class ProblemObjectData
    {
        public string name;
        public string type = "object";
    }

    [Serializable]
    public class ProblemPredicateData
    {
        public string predicate;
        public List<string> args = new List<string>();
        public bool value = true;
    }
}
