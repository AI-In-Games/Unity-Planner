# Guard AI — end-to-end example

A guard that patrols when calm, investigates when suspicious, and raises an alarm when it spots a player. The planner selects which of these behaviours to execute based on the current world state, replanning every second as conditions change.

This example covers the full pipeline: Blackboard, PredicateAdapter, manual action creation, ForwardBitVectorAStar, and a minimal execution loop.

## The scenario

Three modes of behaviour, driven by a suspicion value:

| Suspicion | Goal | Actions available |
|---|---|---|
| 0-50 | reach patrol point | Patrol |
| 51-80 | reach investigate point | Investigate |
| 81-100 | raise alarm | Raise alarm |

The planner does not know about suspicion directly. It knows only what the PredicateAdapter distils from it: the predicates `suspicious` and `alert`.

## Step 1: Predicates

These are the boolean conditions the planner reasons over:

| Predicate | Meaning |
|---|---|
| `player-visible` | the guard can see the player |
| `suspicious` | suspicion > 50 |
| `alert` | suspicion > 80 |
| `at-patrol-point` | guard is physically at a patrol point |
| `at-investigate-point` | guard is physically at an investigate point |
| `alarm-raised` | alarm has been triggered |

## Step 2: Actions

```csharp
private List<GroundedAction> BuildActions()
{
    // Patrol: available when calm
    PlanningState patrolPre = new PlanningState();
    patrolPre.SetPredicate("suspicious", false);
    patrolPre.SetPredicate("alert", false);
    PlanningState patrolEff = new PlanningState();
    patrolEff.SetPredicate("at-patrol-point", true);

    // Investigate: available when suspicious but not alert
    PlanningState investigatePre = new PlanningState();
    investigatePre.SetPredicate("suspicious", true);
    investigatePre.SetPredicate("alert", false);
    PlanningState investigateEff = new PlanningState();
    investigateEff.SetPredicate("at-investigate-point", true);

    // Raise alarm: available when alert
    PlanningState alarmPre = new PlanningState();
    alarmPre.SetPredicate("alert", true);
    PlanningState alarmEff = new PlanningState();
    alarmEff.SetPredicate("alarm-raised", true);

    return new List<GroundedAction>
    {
        new GroundedAction("Patrol",      1f, patrolPre,      patrolEff),
        new GroundedAction("Investigate", 1f, investigatePre, investigateEff),
        new GroundedAction("RaiseAlarm",  1f, alarmPre,       alarmEff),
    };
}
```

## Step 3: Converting game state to predicates

```csharp
private PlanningState GetCurrentState()
{
    // m_Blackboard is updated every frame from perception
    Dictionary<string, bool> predicates = m_Adapter.ExtractPredicates(m_Blackboard);

    PlanningState state = new PlanningState();
    foreach (KeyValuePair<string, bool> kvp in predicates)
        state.SetPredicate(kvp.Key, kvp.Value);

    // Physical location predicates come from the agent's position, not the blackboard
    state.SetPredicate("at-patrol-point",      IsAtAny(m_PatrolPoints));
    state.SetPredicate("at-investigate-point", IsAtAny(m_InvestigatePoints));
    return state;
}
```

## Step 4: Choosing a goal

The goal changes as suspicion rises:

```csharp
private PlanningState GetGoal()
{
    int suspicion = m_Blackboard.GetValue<int>("suspicion");
    PlanningState goal = new PlanningState();

    if (suspicion > 80)
        goal.SetPredicate("alarm-raised", true);
    else if (suspicion > 50)
        goal.SetPredicate("at-investigate-point", true);
    else
        goal.SetPredicate("at-patrol-point", true);

    return goal;
}
```

## Step 5: Planning and execution

```csharp
private void Replan()
{
    PlanningState current = GetCurrentState();
    PlanningState goal    = GetGoal();

    if (current.Satisfies(goal))
        return; // already there, nothing to do

    PlanResult result = m_Planner.Plan(current, goal, m_Actions);

    if (result.Success)
    {
        m_CurrentPlan  = result;
        m_CurrentStep  = 0;
    }
}

private void ExecuteCurrentStep()
{
    if (m_CurrentPlan == null || !m_CurrentPlan.Success)
        return;
    if (m_CurrentStep >= m_CurrentPlan.Actions.Count)
        return;

    GroundedAction action = m_CurrentPlan.Actions[m_CurrentStep];

    if (DispatchAction(action))
        m_CurrentStep++;
}

private bool DispatchAction(GroundedAction action)
{
    switch (action.Name)
    {
        case "Patrol":      return MoveToNearest(m_PatrolPoints);
        case "Investigate": return MoveToNearest(m_InvestigatePoints);
        case "RaiseAlarm":  return TriggerAlarm();
        default:            return true;
    }
}
```

`DispatchAction` returns `true` when the action is complete so the executor advances to the next step.

## Full MonoBehaviour

```csharp
using System.Collections.Generic;
using AiInGames.Blackboard;
using AIInGames.Planning.Runtime;
using UnityEngine;

public class GuardAI : MonoBehaviour
{
    [SerializeField] private Transform[]  m_PatrolPoints;
    [SerializeField] private Transform[]  m_InvestigatePoints;
    [SerializeField] private Transform    m_Player;
    [SerializeField] private float        m_DetectionRange    = 10f;
    [SerializeField] private float        m_ReplanInterval    = 1f;

    private Blackboard       m_Blackboard;
    private PredicateAdapter m_Adapter;
    private ISearchEngine    m_Planner;
    private List<GroundedAction> m_Actions;

    private PlanResult m_CurrentPlan;
    private int        m_CurrentStep;
    private float      m_NextReplan;

    private void Start()
    {
        m_Blackboard = new Blackboard();
        m_Blackboard.SetValue("suspicion",     0);
        m_Blackboard.SetValue("playerVisible", false);

        m_Adapter = new PredicateAdapter();
        m_Adapter.RegisterRule("suspicious",    bb => bb.GetValue<int>("suspicion") > 50);
        m_Adapter.RegisterRule("alert",         bb => bb.GetValue<int>("suspicion") > 80);
        m_Adapter.RegisterRule("player-visible",bb => bb.GetValue<bool>("playerVisible"));

        m_Planner  = new ForwardBitVectorAStar(maxIterations: 1000);
        m_Actions  = BuildActions();

        Replan();
    }

    private void Update()
    {
        UpdatePerception();

        if (Time.time >= m_NextReplan)
        {
            Replan();
            m_NextReplan = Time.time + m_ReplanInterval;
        }

        ExecuteCurrentStep();
    }

    private void UpdatePerception()
    {
        bool visible = m_Player != null &&
                       Vector3.Distance(transform.position, m_Player.position) < m_DetectionRange;

        m_Blackboard.SetValue("playerVisible", visible);

        int suspicion = m_Blackboard.GetValue<int>("suspicion");
        suspicion += visible ? (int)(30f * Time.deltaTime) : -(int)(10f * Time.deltaTime);
        suspicion  = Mathf.Clamp(suspicion, 0, 100);
        m_Blackboard.SetValue("suspicion", suspicion);
    }

    private void Replan()
    {
        PlanningState current = GetCurrentState();
        PlanningState goal    = GetGoal();

        if (current.Satisfies(goal))
            return;

        PlanResult result = m_Planner.Plan(current, goal, m_Actions);

        if (result.Success)
        {
            m_CurrentPlan = result;
            m_CurrentStep = 0;
        }
    }

    private void ExecuteCurrentStep()
    {
        if (m_CurrentPlan == null || !m_CurrentPlan.Success) return;
        if (m_CurrentStep >= m_CurrentPlan.Actions.Count)   return;

        if (DispatchAction(m_CurrentPlan.Actions[m_CurrentStep]))
            m_CurrentStep++;
    }

    private bool DispatchAction(GroundedAction action)
    {
        switch (action.Name)
        {
            case "Patrol":      return MoveToNearest(m_PatrolPoints);
            case "Investigate": return MoveToNearest(m_InvestigatePoints);
            case "RaiseAlarm":  return TriggerAlarm();
            default:            return true;
        }
    }

    private PlanningState GetCurrentState()
    {
        Dictionary<string, bool> predicates = m_Adapter.ExtractPredicates(m_Blackboard);
        PlanningState state = new PlanningState();
        foreach (KeyValuePair<string, bool> kvp in predicates)
            state.SetPredicate(kvp.Key, kvp.Value);
        state.SetPredicate("at-patrol-point",      IsAtAny(m_PatrolPoints));
        state.SetPredicate("at-investigate-point", IsAtAny(m_InvestigatePoints));
        return state;
    }

    private PlanningState GetGoal()
    {
        int suspicion = m_Blackboard.GetValue<int>("suspicion");
        PlanningState goal = new PlanningState();
        if      (suspicion > 80) goal.SetPredicate("alarm-raised",          true);
        else if (suspicion > 50) goal.SetPredicate("at-investigate-point",  true);
        else                     goal.SetPredicate("at-patrol-point",        true);
        return goal;
    }

    private List<GroundedAction> BuildActions()
    {
        PlanningState patrolPre = new PlanningState();
        patrolPre.SetPredicate("suspicious", false);
        patrolPre.SetPredicate("alert", false);
        PlanningState patrolEff = new PlanningState();
        patrolEff.SetPredicate("at-patrol-point", true);

        PlanningState investigatePre = new PlanningState();
        investigatePre.SetPredicate("suspicious", true);
        investigatePre.SetPredicate("alert", false);
        PlanningState investigateEff = new PlanningState();
        investigateEff.SetPredicate("at-investigate-point", true);

        PlanningState alarmPre = new PlanningState();
        alarmPre.SetPredicate("alert", true);
        PlanningState alarmEff = new PlanningState();
        alarmEff.SetPredicate("alarm-raised", true);

        return new List<GroundedAction>
        {
            new GroundedAction("Patrol",      1f, patrolPre,      patrolEff),
            new GroundedAction("Investigate", 1f, investigatePre, investigateEff),
            new GroundedAction("RaiseAlarm",  1f, alarmPre,       alarmEff),
        };
    }

    private bool IsAtAny(Transform[] points)
    {
        foreach (Transform p in points)
            if (Vector3.Distance(transform.position, p.position) < 1f)
                return true;
        return false;
    }

    private bool MoveToNearest(Transform[] points)
    {
        if (points == null || points.Length == 0) return true;

        Transform nearest  = points[0];
        float     minDist  = float.MaxValue;
        foreach (Transform p in points)
        {
            float d = Vector3.Distance(transform.position, p.position);
            if (d < minDist) { minDist = d; nearest = p; }
        }

        transform.position = Vector3.MoveTowards(
            transform.position, nearest.position, 3f * Time.deltaTime);

        return minDist < 0.5f;
    }

    private bool TriggerAlarm()
    {
        Debug.Log("Alarm raised!");
        // Play alarm audio, notify other guards, etc.
        return true;
    }
}
```

## Unity setup

1. Create an empty scene with a ground plane.
2. Add a capsule for the guard and attach the `GuardAI` script.
3. Add a sphere for the player and assign it to the `Player` field.
4. Create three or four empty GameObjects spread around the scene and assign them to `Patrol Points`.
5. Create two empty GameObjects near the centre and assign them to `Investigate Points`.
6. Enter Play mode. The guard will patrol. Walk the player within 10 units and hold position — suspicion will rise, the guard will switch to investigating, and eventually raise the alarm.

## What to look at in the output

- `PlanResult.Actions.Count` is almost always 1 here because the plan is one step: move to the current goal. The planner runs again each second as the goal changes.
- `PlanResult.NodesExpanded` should stay in the single digits for this domain. If it grows, an action or predicate is probably misconfigured.
- If replanning returns `Success=false`, check that the goal state is achievable with the current action set. A common mistake is setting a precondition that can never be satisfied given the initial state.

## Next steps

- Replace `MoveToNearest` with a NavMesh agent call so the guard navigates properly around obstacles.
- Add a `call-backup` action with a higher cost to model preference for solo investigation before requesting help.
- Try `CompiledDomain` caching if you have many guards running the same action set — compile once and share.
- Add `PlanExecutor` instead of the manual `m_CurrentStep` counter if you want event callbacks and mid-execution failure detection built in.
