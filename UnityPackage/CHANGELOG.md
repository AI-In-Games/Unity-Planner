# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Structured grounded-action arguments: `GroundedAction.ActionName`, `Arguments`, and `Argument(name)`, so consumers read parameters by name instead of parsing the formatted action string
- Opt-in compiled-domain cache diagnostics (hit/miss tally), gated behind the `PLANNING_DEBUG` scripting define
- Bottom, collapsible Problems panel in the Domain Editor that reports validation issues live

### Changed
- Domain Editor PDDL actions consolidated under a single PDDL dropdown: Preview now prints to the Console (no popup), Export saves a `.pddl` file
- `PlanExecutor` matches executors by action name (exact match, then longest registered prefix) instead of prefixing the formatted action string, removing nondeterministic collisions
- Optional VAL validation is now provided by the `com.aiingames.pddl-parser` package behind the `ENABLE_VALIDATION` define

### Removed
- In-package VAL runner and bundled binaries (moved to the PDDL parser package)
- Bundled samples
- Assorted development and test menu items, and the Export PDDL button on the DomainAsset inspector (export is in the Domain Editor's PDDL dropdown)

### Planned
- Multi-domain support
- Action cost visualization
- Heuristic function editor

## [0.1.0] - TBD

### Added
- List-based Domain Editor for PDDL domains (actions primary; predicates and types secondary)
- ScriptableObject-based domain assets with JSON serialization
- PDDL import (via the parser package) and PDDL preview/export
- Action editor with parameters, preconditions (AND/OR/NOT), and effects
- Predicate and type definition panels with a type hierarchy tree
- Reactive domain validation (ten rules) surfaced as errors and warnings
- Undoable editing throughout
- Forward bit-vector and dictionary A* planners with compiled-domain caching
- PlanExecutor for stepping through plans with event callbacks

### Technical
- Depends on the PDDL-Parser package for parsing and serialization
- UIToolkit (UXML/USS) editor, custom inspectors for planning assets
