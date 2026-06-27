# AI In Games — Planning

A goal-oriented action planner for Unity games. You describe your AI's world as a set of boolean conditions, define actions with preconditions and effects, set a goal state, and the planner finds the action sequence to reach it.

> ⚠️ **Proof of concept — not production ready.** This is a research prototype. APIs may change, and it has not been hardened or tested for production use. Use it to explore and learn from, not to ship.

## When to use it

Use this package when your AI needs to reason about goals and consequences at runtime. It fits well for combat AI that adapts to resources and health, RTS agents that coordinate gathering and construction, stealth AI that responds to alert states, and companion AI with multi-step task chains.

Behavior trees are better when the logic is fixed and hand-authored. This planner is better when the right sequence of actions changes based on world state.

## Features

- Forward A* over a compact bit-vector state representation — fast enough for real-time replanning across multiple agents
- Typed state values (IntKey, FloatKey, StringKey) for domains that need numeric reasoning alongside boolean predicates
- Parameterized action grounding — define action templates once, ground them to concrete object combinations at runtime
- PDDL domain import — load planning domains and problems from standard .pddl files
- `PlanExecutor` for stepping through a plan with event callbacks and mid-execution failure detection
- `CompiledDomain` caching — pre-process action masks once so repeat planning on the same domain skips the compile step

## Installation

In `Packages/manifest.json`, add a local path reference:

```json
"com.aiingames.planning": "file:../../UnityPackage"
```

Or a git URL:

```json
"com.aiingames.planning": "https://github.com/AI-In-Games/Planning.git?path=/UnityPackage"
```

## Minimal example

```csharp
using AIInGames.Planning.Runtime;
using System.Collections.Generic;

// World state
PlanningState initial = new PlanningState();
initial.SetPredicate("at-base", true);

PlanningState goal = new PlanningState();
goal.SetPredicate("enemy-dead", true);

// Actions
PlanningState movePre = new PlanningState();
movePre.SetPredicate("at-base", true);
PlanningState moveEff = new PlanningState();
moveEff.SetPredicate("at-base", false);
moveEff.SetPredicate("at-enemy", true);

PlanningState attackPre = new PlanningState();
attackPre.SetPredicate("at-enemy", true);
PlanningState attackEff = new PlanningState();
attackEff.SetPredicate("enemy-dead", true);

List<GroundedAction> actions = new List<GroundedAction>
{
    new GroundedAction("Move",   1f, movePre,   moveEff),
    new GroundedAction("Attack", 1f, attackPre, attackEff),
};

// Plan
PlanResult result = new ForwardBitVectorAStar().Plan(initial, goal, actions);

if (result.Success)
{
    foreach (GroundedAction action in result.Actions)
        Debug.Log(action.Name); // Move, Attack
}
```

## Documentation

- [Quick Start](Documentation~/quick-start.md) — a working plan in five minutes, then the playable Soldier demos
- [Getting Started](Documentation~/getting-started.md) — core concepts and your first plan
- [Architecture](Documentation~/architecture.md) — how the components fit together
- [Domain Editor](Documentation~/domain-editor.md) — authoring PDDL domains visually
- [Guard AI example](Documentation~/guard-ai.md) — complete end-to-end example in Unity
- [Plan Executor Guide](../Docs/PlanExecutorGuide.md) — executing plans with event callbacks
- [Action Grounding Guide](../Docs/ActionGroundingGuide.md) — parameterized action templates
- [Predicate Integration](../Docs/PredicateIntegration.md) — converting game state to predicates

## Contributions

Contributions are welcome! Bug reports, feature ideas, documentation improvements, and pull requests are all appreciated. Open an issue to discuss larger changes before starting.
