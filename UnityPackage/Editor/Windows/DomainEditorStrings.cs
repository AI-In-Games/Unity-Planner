namespace AIInGames.Planning.Unity.Editor
{
    internal static class DomainEditorStrings
    {
        internal static class Tooltips
        {
            // Domain-level navigation
            public const string ActionsSection    = "Actions define what an agent can do. Each action has typed parameters, preconditions that must hold before it executes, and effects that change the world state.";
            public const string AddAction         = "Add a new action to this domain.";
            public const string PredicatesFoldout = "Predicates are named boolean facts that describe the world state. They appear as preconditions and effects in actions.";
            public const string AddPredicate      = "Add a new predicate to this domain.";
            public const string TypesFoldout      = "Object types define a type hierarchy for the objects in this domain. Action parameters are typed so the planner knows which objects can fill each role.";
            public const string AddType           = "Add a new object type to this domain.";

            // Toolbar
            public const string Validate    = "Run full VAL type-checking on the domain and print the detailed report to the console.";
            public const string Pddl        = "PDDL output: preview the domain in the Console, or export it to a .pddl file.";

            // Action editor — sections  (:parameters / :precondition / :effect in PDDL)
            public const string ParametersSection    = "Parameters (:parameters in PDDL) are the typed variables the planner binds to domain objects when it instantiates this action.";
            public const string PreconditionsSection = "Preconditions (:precondition in PDDL) specify what must hold true for this action to be applicable. All top-level conditions form an implicit AND — every one must be satisfied simultaneously.";
            public const string EffectsSection       = "Effects (:effect in PDDL) describe how the world state changes when this action executes. IS (Add) makes a predicate true; NOT (Remove) makes it false.";

            // Action editor — inline buttons
            public const string AddParameter   = "Add a new typed parameter to this action.";
            public const string AddPrecondition = "Add a precondition. Choose a predicate (positive or negated), or an AND / OR group. AND/OR groups can be nested up to three levels deep.";
            public const string AddEffect       = "Add a new effect to this action.";

            // Condition row
            public const string NegateCondition = "Toggle between IS (predicate must be true) and NOT (predicate must be false). PDDL notation: (not (predicate ...)).";
        }

        internal static class Labels
        {
            public const string PredicatesFoldoutFormat = "Predicates ({0})";
            public const string TypesFoldoutFormat      = "Object Types ({0})";
        }
    }
}
