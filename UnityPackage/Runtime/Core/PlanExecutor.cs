using System;
using System.Collections.Generic;

namespace AIInGames.Planning.Runtime
{
    public class PlanExecutor
    {
        public enum ExecutionState
        {
            Idle,
            Executing,
            Completed,
            Failed
        }

        public ExecutionState State { get; private set; }
        public PlanResult CurrentPlan { get; private set; }
        public int CurrentActionIndex { get; private set; }
        public GroundedAction CurrentAction =>
            CurrentPlan != null && CurrentActionIndex < CurrentPlan.Actions.Count
                ? CurrentPlan.Actions[CurrentActionIndex]
                : null;

        public event Action<GroundedAction> OnActionStarted;
        public event Action<GroundedAction> OnActionCompleted;
        public event Action<GroundedAction, string> OnActionFailed;
        public event Action OnPlanCompleted;
        public event Action<string> OnPlanFailed;

        private readonly Dictionary<string, IActionExecutor> m_Executors;
        private IActionExecutor m_CurrentExecutor;

        public PlanExecutor()
        {
            m_Executors = new Dictionary<string, IActionExecutor>(StringComparer.Ordinal);
            State = ExecutionState.Idle;
        }

        public void RegisterExecutor(string actionPrefix, IActionExecutor executor)
        {
            m_Executors[actionPrefix.ToLower()] = executor;
        }

        public void StartPlan(PlanResult plan)
        {
            if (plan == null || !plan.Success)
            {
                State = ExecutionState.Failed;
                OnPlanFailed?.Invoke("Invalid or failed plan");
                return;
            }

            if (plan.Actions.Count == 0)
            {
                State = ExecutionState.Completed;
                OnPlanCompleted?.Invoke();
                return;
            }

            CurrentPlan = plan;
            CurrentActionIndex = 0;
            State = ExecutionState.Executing;

            StartNextAction();
        }

        public void Update()
        {
            if (State != ExecutionState.Executing)
                return;

            if (m_CurrentExecutor == null)
                return;

            if (m_CurrentExecutor.HasFailed())
            {
                GroundedAction failedAction = CurrentAction;
                string reason = $"Action failed during execution: {failedAction.Name}";
                m_CurrentExecutor = null;
                State = ExecutionState.Failed;
                OnActionFailed?.Invoke(failedAction, reason);
                OnPlanFailed?.Invoke(reason);
                return;
            }

            if (m_CurrentExecutor.IsComplete())
            {
                CompleteCurrentAction();
            }
        }

        public void Cancel()
        {
            if (State != ExecutionState.Executing)
                return;

            if (m_CurrentExecutor != null)
            {
                m_CurrentExecutor.Cancel();
                m_CurrentExecutor = null;
            }

            State = ExecutionState.Idle;
            CurrentPlan = null;
            CurrentActionIndex = 0;
        }

        private void StartNextAction()
        {
            if (CurrentActionIndex >= CurrentPlan.Actions.Count)
            {
                State = ExecutionState.Completed;
                OnPlanCompleted?.Invoke();
                return;
            }

            GroundedAction action = CurrentPlan.Actions[CurrentActionIndex];
            IActionExecutor executor = FindExecutor(action);

            if (executor == null)
            {
                State = ExecutionState.Failed;
                OnActionFailed?.Invoke(action, $"No executor found for action: {action.Name}");
                OnPlanFailed?.Invoke($"No executor for action: {action.Name}");
                return;
            }

            if (!executor.CanExecute(action))
            {
                State = ExecutionState.Failed;
                OnActionFailed?.Invoke(action, $"Executor cannot execute action: {action.Name}");
                OnPlanFailed?.Invoke($"Cannot execute action: {action.Name}");
                return;
            }

            m_CurrentExecutor = executor;
            executor.StartExecution(action);
            OnActionStarted?.Invoke(action);
        }

        private void CompleteCurrentAction()
        {
            GroundedAction completedAction = CurrentAction;
            OnActionCompleted?.Invoke(completedAction);

            m_CurrentExecutor = null;
            CurrentActionIndex++;

            StartNextAction();
        }

        // Matches on the structured action name (the schema name, not the formatted "name(args)"
        // string). An exact registration wins; otherwise the longest registered prefix that the name
        // starts with is used, so one executor can serve a family (for example "heal" for
        // "heal-to-mid" and "heal-to-full") while a more specific registration always takes priority.
        private IActionExecutor FindExecutor(GroundedAction action)
        {
            string actionName = action.ActionName.ToLower();

            if (m_Executors.TryGetValue(actionName, out IActionExecutor exact))
                return exact;

            IActionExecutor best = null;
            int bestPrefixLength = -1;
            foreach (var kvp in m_Executors)
            {
                if (kvp.Key.Length > bestPrefixLength &&
                    actionName.StartsWith(kvp.Key, StringComparison.Ordinal))
                {
                    best = kvp.Value;
                    bestPrefixLength = kvp.Key.Length;
                }
            }

            return best;
        }
    }
}
