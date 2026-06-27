namespace AIInGames.Planning.Unity.Editor.DomainValidation
{
    /// <summary>
    /// A single validation finding on a domain. ActionName is null for domain-level issues.
    /// </summary>
    public readonly struct DomainIssue
    {
        public IssueSeverity Severity  { get; }
        public string        ActionName { get; }
        public string        Message   { get; }

        private DomainIssue(IssueSeverity severity, string actionName, string message)
        {
            Severity   = severity;
            ActionName = actionName;
            Message    = message;
        }

        public static DomainIssue Error(string actionName, string message)
            => new DomainIssue(IssueSeverity.Error, actionName, message);

        public static DomainIssue Warning(string actionName, string message)
            => new DomainIssue(IssueSeverity.Warning, actionName, message);

        public override string ToString() => $"[{Severity}] {Message}";
    }
}
