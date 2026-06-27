# Domain Editor - User Guide

## Overview

The Domain Editor is a visual tool for authoring PDDL domains stored as `DomainAsset` ScriptableObjects. It is a list-based editor: actions are the primary concept, and predicates and object types are secondary. Every change is undoable, and a problems panel reports validation issues live.

## Opening the editor

Either:
- Menu: `Window > AI Planning > Domain Editor`, then assign a `DomainAsset`, or
- Select a `DomainAsset` in the Project window and click "Open in Domain Editor" in its inspector.

## Layout

```
┌─ Toolbar: Domain: <name>                    [ Validate ] [ PDDL ▾ ] ─┐
├───────────────┬──────────────────────────────────────────────────────┤
│ ACTIONS  +Add │                                                        │
│ [filter...]   │   Editor for the selected action / predicate / type    │
│ • move  3p2r2e│                                                        │
│ • shoot 3p3r1e│                                                        │
│               │                                                        │
│ ▸ Predicates  │                                                        │
│ ▸ Object Types│                                                        │
├───────────────┴──────────────────────────────────────────────────────┤
│ ▾ Problems: 1 error(s), 0 warning(s)                                   │
│   ERROR  [move] ...                                                     │
└───────────────────────────────────────────────────────────────────────┘
```

- **Toolbar**: the domain name, a `Validate` button (external VAL, see below), and a `PDDL` dropdown.
- **Left navigation**: the Actions list (with a filter box and `+ Add`), plus collapsible Predicates and Object Types sections, each with `+ Add`.
- **Center**: the editor for whatever is selected.
- **Bottom**: a collapsible Problems panel listing all validation issues.

## Editing actions

Select an action to edit it. Each action shows the action name, a Delete button, and three sections that map directly to PDDL:

- **Parameters** (`:parameters`): typed variables the planner binds to objects, for example `?a - agent`.
- **Preconditions** (`:precondition`): the facts that must hold for the action to apply. Top-level conditions form an implicit AND. Each row is a predicate (which you can negate) or an AND / OR group; groups can nest up to three levels.
- **Effects** (`:effect`): how the world changes. Each effect is a predicate set to IS (add, makes it true) or NOT (remove, makes it false).

The action list shows a compact `Np · Rr · Ee` summary (parameters, preconditions/requirements, effects).

## Editing predicates and object types

- **Predicates** are named boolean facts with typed parameters. They are what preconditions and effects reference.
- **Object Types** form a hierarchy under the implicit PDDL `object` root, shown as a tree.

Both are added from their foldout's `+ Add` button and edited in the center panel.

## Validation (the Problems panel)

The bottom Problems panel runs a set of reactive rules over the whole domain and updates as you edit. Issues are shown with a severity (ERROR or WARN) and the offending action; clicking an action issue selects that action so you can fix it. The panel is collapsible and its header shows the counts.

Rules include empty or duplicate names, undefined type or predicate references, arity mismatches, contradictory or duplicate conditions, canceling or duplicate effects, no-op effects, and actions with no effects.

### External VAL (optional)

The `Validate` button runs the external VAL tool for full type-checking. This is provided by the `com.aiingames.pddl-parser` package and is gated behind the `ENABLE_VALIDATION` scripting define. To use it, add `ENABLE_VALIDATION` to Project Settings > Player > Scripting Define Symbols; without it, the button reports that validation is disabled. The reactive Problems panel works regardless.

## PDDL output

The `PDDL` dropdown has two actions:
- **Preview (Console)**: writes the domain serialized as PDDL to the Console.
- **Export to File...**: saves the domain as a `.pddl` file on disk.

## Undo

Every modifying action (add, delete, rename, parameter, precondition, and effect edits) is recorded with Unity's undo system, so `Ctrl+Z` reverts the last change and the editor reflects it immediately.
