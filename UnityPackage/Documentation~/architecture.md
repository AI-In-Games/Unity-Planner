# Architecture

## The planning pipeline

```
Game state
(MonoBehaviour fields, sensors, blackboard)
           |
           | PredicateAdapter.ExtractPredicates()
           |
           v
   PlanningState          PlanningState
     (initial)  --------    (goal)
           |                    |
           +--------+----------+
                    |
                    v
        ISearchEngine.Plan(initial, goal, actions)
                    |
             +------+------+
             |             |
   ForwardBitVectorAStar   ForwardDictionaryAStar
   (default, fast)         (typed values, fallback)
             |             |
             +------+------+
                    |
                    v
              PlanResult
     .Success      .Actions
     .TotalCost    .NodesExpanded
     .ElapsedMs    .Iterations
                    |
                    v
            PlanExecutor
    (steps through actions, fires events)
                    |
                    v
       IActionExecutor  (your code)
     MoveAction, AttackAction, etc.
```

## Component roles

**PlanningState** holds the world description. It stores boolean predicates as a string-to-bool dictionary and optional typed values under strongly-typed keys. Two states can be compared for equality or checked for goal satisfaction via `Satisfies()`.

**GroundedAction** is a concrete, fully instantiated action ready for the planner to apply. It has a name, a cost, a precondition state, and an effects state. You create these directly in code or by running `ActionGrounder` over an `ActionDefinition` list.

**ISearchEngine** is the planning entry point. `ForwardBitVectorAStar` is the default and handles nearly all cases. It automatically falls back to `ForwardDictionaryAStar` when the state contains typed values.

**PlanResult** carries the outcome. Check `.Success` first, then iterate `.Actions` in order. `.NodesExpanded` tells you how much search effort was spent, which is useful for diagnosing slow planning.

**PlanExecutor** manages execution lifecycle — it steps through the action list, calls your `IActionExecutor` implementations, raises events on completion or failure, and gives you the hook to trigger replanning.

**CompiledDomain** is a pre-processed representation of your action set. The bit-vector search compiles action masks (precondition and effect bits) from the grounded action list on first use. Passing a cached `CompiledDomain` to the planner skips that step on subsequent calls.

**ActionGrounder** expands parameterized `ActionDefinition` templates into concrete `GroundedAction` instances by binding typed planning objects to action parameters. For a domain with 5 actions and 10 objects, this can produce hundreds of grounded actions.

## Two search paths

```
ForwardBitVectorAStar.Plan(initial, goal, actions)
           |
           | initial.HasTypedValues || goal.HasTypedValues ?
           |
       NO -+           YES -+
           |                |
           v                v
  BitVectorSearchSpace   ForwardDictionaryAStar
  Predicates packed into (handles int, float,
  ulong[] words. State    bool, string values
  comparison and goal     as well as predicates)
  checking via bitwise
  AND/XOR operations.
           |                |
           +---------+------+
                     |
                     v
        AStarEngine<TSpace, TState, THeuristic>
       (single generic engine — JIT specialises
        a separate code path per type triple,
        eliminating virtual dispatch in the loop)
```

Use predicates whenever possible. The bit-vector path packs the entire state into a handful of 64-bit words. State comparison and goal checking become bitwise operations, and the closed set is a hash of those words. The dictionary path allocates per-state and operates on `Dictionary<Type, T>` entries, which is significantly slower.

## Heuristics

The planner uses an admissible or inadmissible heuristic to order the open list. The default is `HAddLiteHeuristic`, which estimates cost-to-goal by summing the cheapest achiever cost for each unmet goal predicate. It is inadmissible (overestimates), which is intentional — it reduces node expansions significantly at the cost of strict optimality.

Available heuristics:

| Heuristic | Description |
|---|---|
| `HAddLiteHeuristic` (default) | Sum of cheapest achiever cost per unmet goal predicate |
| `UnmetPredicateHeuristic` | Count of unmet goal conditions — simpler, weaker guidance |
| `HAddLiteBitHeuristic` | Bit-vector equivalent of HAddLite |
| `UnmetGoalCountBitHeuristic` | Bit-vector unmet count |
| `ZeroHeuristic<T>` | No heuristic — degrades A* to Dijkstra |

To supply a custom heuristic, implement `IHeuristic<PlanningState>` and pass it to `ForwardDictionaryAStar`:

```csharp
public readonly struct MyHeuristic : IHeuristic<PlanningState>
{
    public float Compute(PlanningState state) { /* your estimate */ }
}

var planner = new ForwardDictionaryAStar(new MyHeuristic());
```

## Duplicate handling

`DuplicateHandling` controls how the open and closed sets interact:

- `ClosedSet` — standard A*. A state already expanded is never re-expanded. Correct for admissible heuristics.
- `Dominance` — a state is pruned if a previously seen state satisfies its predicate set as a superset (the seen state is "at least as good"). Used by default on the bit-vector path. Gives better pruning in practice for inadmissible heuristics.

## Burst and the Job System

`NativePlannerDispatcher` wraps a `PlanJob : IJob` that runs the full A* search in a Burst-compiled worker thread. It accepts a `CompiledDomain` and `NativePlannerData` (a flattened native representation of the domain), runs synchronously via `Plan()` or asynchronously via `Schedule()`/`Complete()`, and supports cancellation via `NativeReference<bool>`.

Use this path when you need deterministic frame-time budgets or want to plan for multiple agents in parallel across worker threads.
