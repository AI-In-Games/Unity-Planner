# Getting Started

## Core concepts

A `PlanningState` is a snapshot of the world — a set of named boolean predicates, optionally augmented with typed numeric values. A `GroundedAction` describes something the AI can do: a cost, a set of preconditions (what must be true to apply it), and a set of effects (what changes when it runs). The planner receives an initial state, a goal state, and a list of actions, then searches for the cheapest sequence of actions that transforms the initial state into one that satisfies the goal.

## Part 1: Your first plan

```csharp
using AIInGames.Planning.Runtime;
using System.Collections.Generic;

// Describe the world right now
PlanningState initial = new PlanningState();
initial.SetPredicate("has-wood", false);
initial.SetPredicate("has-axe", true);

// Describe what you want to achieve
PlanningState goal = new PlanningState();
goal.SetPredicate("building-built", true);

// Define the actions
PlanningState chopPre = new PlanningState();
chopPre.SetPredicate("has-axe", true);
PlanningState chopEff = new PlanningState();
chopEff.SetPredicate("has-wood", true);

PlanningState buildPre = new PlanningState();
buildPre.SetPredicate("has-wood", true);
PlanningState buildEff = new PlanningState();
buildEff.SetPredicate("building-built", true);

List<GroundedAction> actions = new List<GroundedAction>
{
    new GroundedAction("chop-wood", 1f, chopPre, chopEff),
    new GroundedAction("build",     1f, buildPre, buildEff),
};

// Run the planner
PlanResult result = new ForwardBitVectorAStar().Plan(initial, goal, actions);

// Read the result
Debug.Log($"Success: {result.Success}");           // True
Debug.Log($"Steps: {result.Actions.Count}");       // 2
Debug.Log($"Cost: {result.TotalCost}");            // 2
Debug.Log($"Nodes: {result.NodesExpanded}");       // search effort
```

The planner returns immediately with the result. `PlanResult.Actions` is the ordered list of actions to execute.

## Part 2: Game state to predicates

In a real game, world state lives in MonoBehaviour fields, a blackboard, or sensor components — not in a `PlanningState`. `PredicateAdapter` bridges that gap by mapping arbitrary game data to named boolean predicates.

```csharp
using AiInGames.Blackboard;
using AIInGames.Planning.Runtime;
using System.Collections.Generic;

// Game state lives in a Blackboard
Blackboard blackboard = new Blackboard();
blackboard.SetValue("health", 30);
blackboard.SetValue("hasWeapon", true);
blackboard.SetValue("enemyVisible", true);

// Map game data to predicates
PredicateAdapter adapter = new PredicateAdapter();
adapter.RegisterRule("health-low",    bb => bb.GetValue<int>("health") < 50);
adapter.RegisterRule("has-weapon",    bb => bb.GetValue<bool>("hasWeapon"));
adapter.RegisterRule("enemy-visible", bb => bb.GetValue<bool>("enemyVisible"));

// Extract the current predicate snapshot
Dictionary<string, bool> predicates = adapter.ExtractPredicates(blackboard);

PlanningState state = new PlanningState();
foreach (KeyValuePair<string, bool> kvp in predicates)
    state.SetPredicate(kvp.Key, kvp.Value);

// state now has: health-low=true, has-weapon=true, enemy-visible=true
```

Call `ExtractPredicates` each time you need a fresh snapshot before planning.

## Part 3: Negative preconditions and the closed-world assumption

The planner uses the closed-world assumption: a predicate that is absent from a state is treated as `false`. This means you do not need to explicitly set every predicate to `false` — you only need to set the ones that are `true`, and leave the rest unset.

Negative preconditions work naturally as a result:

```csharp
// "I can flee only when I am not in cover"
PlanningState fleePre = new PlanningState();
fleePre.SetPredicate("in-cover", false); // satisfied when in-cover is absent OR explicitly false
```

If you set a predicate to `false` in the preconditions and the current state either lacks that predicate or has it set to `false`, the precondition is satisfied.

## Part 4: Typed values

`ForwardBitVectorAStar` only understands boolean predicates. If you pass a state with typed values (`IntKey`, `FloatKey`, `StringKey`), it will log a warning and return failure. Use `ForwardDictionaryAStar` if you need typed values:

```csharp
public class Health : IntKey { }

PlanningState state = new PlanningState();
state.Set<Health>(30);

PlanningState goal = new PlanningState();
goal.Set<Health>(100);

// ForwardDictionaryAStar supports typed values
PlanResult result = new ForwardDictionaryAStar().Plan(state, goal, actions);
```

`ForwardDictionaryAStar` is significantly slower than `ForwardBitVectorAStar` on real domains. The recommended approach is to avoid typed values entirely and model numeric thresholds as boolean predicates via `PredicateAdapter`:

```csharp
adapter.RegisterRule("health-low",  bb => bb.GetValue<int>("health") < 30);
adapter.RegisterRule("health-full", bb => bb.GetValue<int>("health") >= 100);
```

This keeps the fast bit-vector path and makes threshold conditions explicit and readable. Reserve `ForwardDictionaryAStar` for cases where discrete boolean modeling genuinely does not fit.

## Part 5: Executing the plan

`PlanResult.Actions` is a list you iterate yourself, or you can use `PlanExecutor` which handles step-by-step execution with callbacks.

Manual approach:

```csharp
if (result.Success)
{
    foreach (GroundedAction action in result.Actions)
    {
        // Dispatch to your own action handlers
        ExecuteAction(action.Name);
    }
}
```

With `PlanExecutor`:

```csharp
PlanExecutor executor = new PlanExecutor();
executor.OnActionStarted   += action => Debug.Log($"Starting: {action.Name}");
executor.OnActionCompleted += action => Debug.Log($"Done: {action.Name}");
executor.OnPlanCompleted   += () => Debug.Log("Plan complete");
executor.OnPlanFailed      += () => Replan();

executor.StartPlan(result, GetActionExecutors());
```

See the [Plan Executor Guide](../../Docs/PlanExecutorGuide.md) for a full walkthrough including mid-execution failure handling and replanning.

## Common mistakes

**Predicate absent vs explicitly false.** Because of the closed-world assumption, a predicate absent from the initial state is `false`. This usually does what you want, but be careful when your `PredicateAdapter` rules can return `false` — that writes an explicit `false` into the state, which is equivalent to the predicate being absent for planning purposes.

**Goal too strict.** A goal state only needs to contain the predicates you care about. If you add more predicates than necessary to the goal, the planner must satisfy all of them, which restricts valid solutions.

**MaxIterations too low.** The default is 10,000 node expansions. For large action spaces or long plans, increase it: `new ForwardBitVectorAStar(maxIterations: 50000)`. If the planner returns `Success=false` on a domain that should be solvable, this is the first thing to check.

**Typed values with the wrong planner.** `ForwardBitVectorAStar` does not support typed values and will warn and fail if you pass them. Use `ForwardDictionaryAStar` for typed-value domains, or model numeric thresholds as boolean predicates to keep the fast bit-vector path.
