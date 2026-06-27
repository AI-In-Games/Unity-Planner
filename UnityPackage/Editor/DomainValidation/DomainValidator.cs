using System.Collections.Generic;
using AIInGames.Planning.Unity;
using AIInGames.Planning.Unity.Editor.DomainValidation.Rules;

namespace AIInGames.Planning.Unity.Editor.DomainValidation
{
    /// <summary>
    /// Runs a configurable set of validation rules against a DomainAsset and collects issues.
    /// Construct with the default ruleset or supply your own for testing.
    /// </summary>
    public sealed class DomainValidator
    {
        private readonly IReadOnlyList<IDomainValidationRule> _rules;

        public DomainValidator() : this(DefaultRules()) { }

        public DomainValidator(IReadOnlyList<IDomainValidationRule> rules)
        {
            _rules = rules;
        }

        public IReadOnlyList<DomainIssue> Validate(DomainAsset domain)
        {
            var issues = new List<DomainIssue>();
            foreach (var rule in _rules)
                foreach (var issue in rule.Check(domain))
                    issues.Add(issue);
            return issues;
        }

        private static IReadOnlyList<IDomainValidationRule> DefaultRules()
        {
            return new IDomainValidationRule[]
            {
                new EmptyNamesRule(),
                new DuplicateNamesRule(),
                new UndefinedTypeReferencesRule(),
                new UndefinedPredicateReferencesRule(),
                new DuplicateConditionsRule(),
                new ContradictoryConditionsRule(),
                new DuplicateEffectsRule(),
                new CancelingEffectsRule(),
                new NoOpEffectsRule(),
                new NoEffectsRule(),
            };
        }
    }
}
