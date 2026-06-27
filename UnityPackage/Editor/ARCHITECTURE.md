# Domain Editor Architecture

## Overview

The editor tooling is built on Unity's UIToolkit (UXML + USS), not GraphView. The main window is a list-based editor for `DomainAsset` ScriptableObjects: actions are primary, predicates and types are secondary, all edits go through Unity's undo system, and a rule-based validator feeds a problems panel.

## Components

```
Editor/
├── Windows/
│   ├── DomainEditorWindow.cs     # main window: nav lists, action/predicate/type editors, problems panel
│   ├── DomainEditorStrings.cs    # tooltip and label strings
│   ├── DomainPddlActions.cs      # PDDL preview (to Console) and export (to file)
│   └── PlanVisualizerWindow.cs   # separate window for visualizing a plan
├── Inspectors/
│   └── DomainAssetEditor.cs      # custom inspector: "Open in Domain Editor"
├── Core/
│   └── UndoScope.cs              # IDisposable: Undo.RecordObject on enter, SetDirty on exit
├── Services/
│   ├── TypeValidator.cs          # ITypeValidator: type-graph checks
│   ├── TypeUsageAnalyzer.cs      # ITypeUsageAnalyzer: where a type is used
│   └── TypeHierarchyBuilder.cs   # pure builder of the type tree (unit tested)
├── DomainValidation/
│   ├── DomainValidator.cs        # runs the rule set, returns IReadOnlyList<DomainIssue>
│   ├── DomainIssue.cs            # { Severity, ActionName, Message }
│   ├── IDomainValidationRule.cs  # rule interface (injectable, testable)
│   └── Rules/                    # ten rules (see below)
├── Extensions/                   # DomainAsset helpers
├── PlanInputParser.cs, PlanRunner.cs, ProblemConverter.cs
└── Resources/
    ├── UXML/                     # DomainEditorWindow, ActionEditor, ConditionRow, EffectRow, ...
    └── USS/                      # DomainEditorStyles
```

## DomainEditorWindow

`CreateGUI` clones `Resources/UXML/DomainEditorWindow.uxml` into the root and adds `Resources/USS/DomainEditorStyles.uss`. The layout is:

- a toolbar (domain label, Validate, PDDL dropdown),
- a `TwoPaneSplitView`: left is the navigation panel (a `ListView` of actions with a filter, plus collapsible `Foldout`s holding the predicates `ListView` and the types `TreeView`), right is the main editor panel,
- a collapsible `Foldout` at the bottom: the Problems panel.

Selecting an action clones `ActionEditor.uxml` into the main panel and builds the parameter, precondition, and effect rows from their own UXML templates. Predicate and type editors work the same way.

The actions list binds to a filtered `visibleActions` list (the search box filters by action name), so binding and selection are routed through that, not the raw domain list.

## Editing and undo

Every mutation of the domain is wrapped in `UndoScope(currentDomain, "...")`, which calls `Undo.RecordObject` before the change and `EditorUtility.SetDirty` after. This covers adds, deletes, renames, and parameter, precondition, and effect edits. The window subscribes to `Undo.undoRedoPerformed` and refreshes the UI so `Ctrl+Z` is reflected immediately.

## Validation

`DomainValidator` holds a list of `IDomainValidationRule` and returns `DomainIssue`s with a severity, the action they belong to, and a message. The ten rules are: EmptyNames, DuplicateNames, UndefinedTypeReferences, UndefinedPredicateReferences, DuplicateConditions, ContradictoryConditions, DuplicateEffects, CancelingEffects, NoOpEffects, and NoEffects.

The window renders the issues in the bottom Problems panel. Because the domain is a ScriptableObject with no change event and edits happen across many controls, the panel is refreshed both immediately on the wired edits and by a light poll (`schedule.Execute(...).Every(500)`), rebuilding only when the issue set actually changes. Clicking an action issue selects that action.

External, full type-checking via the VAL tool is provided by the `com.aiingames.pddl-parser` package (`ValPlanValidator`), gated behind the `ENABLE_VALIDATION` scripting define and invoked from the Validate button.

## PDDL output

`DomainPddlActions` is a static helper kept out of the window so it stays independently testable. `LogPreview` writes `DomainSerializer.ToPddl(domain)` to the Console; `Export` writes it to a chosen `.pddl` file. The window's PDDL dropdown is the only wiring.
