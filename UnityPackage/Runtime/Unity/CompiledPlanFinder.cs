using System;
using System.Collections.Generic;
using AIInGames.Planning.Unity;

namespace AIInGames.Planning.Runtime
{
    /// <summary>
    /// Generic plan finder. It grounds and compiles a domain once per distinct object set through
    /// the supplied backend, caches the result, and reuses it across replans, so a frequently
    /// changing world state pays only for search. This caching logic is independent of which
    /// planner the backend uses.
    /// </summary>
    public sealed class CompiledPlanFinder<TCompiled> : IPlanFinder
    {
        private readonly List<ActionDefinition> m_Actions;
        private readonly IPlannerBackend<TCompiled> m_Backend;
        private readonly Dictionary<string, TCompiled> m_Cache =
            new Dictionary<string, TCompiled>(StringComparer.Ordinal);

        public CompiledPlanFinder(List<ActionDefinition> actions, IPlannerBackend<TCompiled> backend)
        {
            m_Actions = actions ?? throw new ArgumentNullException(nameof(actions));
            m_Backend = backend ?? throw new ArgumentNullException(nameof(backend));
        }

        /// <summary>Distinct object sets compiled and cached so far. Exposed for diagnostics and tests.</summary>
        public int CompiledDomainCount => m_Cache.Count;

        /// <summary>Drops cached compiled domains. Call after the domain's action schemas change.</summary>
        public void ClearCache() => m_Cache.Clear();

        public PlanResult Find(PlanningProblemDefinition problem)
        {
            if (problem == null)
                throw new ArgumentNullException(nameof(problem));

            string key = ObjectSignature(problem.Objects);
            if (m_Cache.TryGetValue(key, out TCompiled compiled))
            {
#if PLANNING_DEBUG
                PlanCacheDiagnostics.RecordHit(key);
#endif
            }
            else
            {
                compiled = m_Backend.Compile(m_Actions, problem.Objects);
                m_Cache[key] = compiled;
#if PLANNING_DEBUG
                PlanCacheDiagnostics.RecordMiss(key, m_Cache.Count);
#endif
            }
            return m_Backend.Search(compiled, problem.InitialState, problem.GoalState);
        }

        // Order-independent signature of the object set: same objects (names plus types) reuse one compile.
        private static string ObjectSignature(IReadOnlyList<PlanningObject> objects)
        {
            List<string> tokens = new List<string>(objects.Count);
            for (int i = 0; i < objects.Count; i++)
                tokens.Add(objects[i].Name + ":" + objects[i].Type);
            tokens.Sort(StringComparer.Ordinal);
            return string.Join(";", tokens);
        }
    }
}
