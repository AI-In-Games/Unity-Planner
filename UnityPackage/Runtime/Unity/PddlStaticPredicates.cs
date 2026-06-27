using System;
using System.Collections.Generic;
using AIInGames.Planning.PDDL;

namespace AIInGames.Planning.Unity
{
    /// <summary>
    /// Static-predicate analysis for PDDL domains. A predicate is static when no action effect ever
    /// adds or removes it. In untyped STRIPS domains, static unary predicates encode object types
    /// (for example (ball ?b)), while dynamic unary predicates are state (for example (free ?g)).
    /// Both importers use this to tell types from state when a domain does not declare types.
    /// </summary>
    internal static class PddlStaticPredicates
    {
        /// <summary>Lowercased names of every predicate that appears in at least one action effect.</summary>
        public static HashSet<string> CollectEffectPredicateNames(IDomain domain)
        {
            HashSet<string> names = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < domain.Actions.Count; i++)
                Collect(domain.Actions[i].Effect, names);
            return names;
        }

        /// <summary>
        /// Same analysis over already-imported actions, so the problem importer can be handed the
        /// domain the caller already parsed instead of re-parsing the domain text.
        /// </summary>
        public static HashSet<string> CollectEffectPredicateNames(IReadOnlyList<ActionDefinition> actions)
        {
            HashSet<string> names = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < actions.Count; i++)
            {
                List<Effect> effects = actions[i].Effects.Effects;
                for (int j = 0; j < effects.Count; j++)
                    names.Add(effects[j].PredicateName.ToLowerInvariant());
            }
            return names;
        }

        private static void Collect(IEffect effect, HashSet<string> names)
        {
            switch (effect.Type)
            {
                case EffectType.Literal:
                    names.Add(effect.Literal.Predicate.Name.ToLowerInvariant());
                    break;
                case EffectType.And:
                    for (int i = 0; i < effect.Children.Count; i++)
                        Collect(effect.Children[i], names);
                    break;
            }
        }
    }
}
