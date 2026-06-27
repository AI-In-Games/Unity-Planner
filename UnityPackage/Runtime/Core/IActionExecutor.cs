namespace AIInGames.Planning.Runtime
{
    public interface IActionExecutor
    {
        bool CanExecute(GroundedAction action);

        void StartExecution(GroundedAction action);

        bool IsComplete();

        bool HasFailed();

        void Cancel();
    }
}
