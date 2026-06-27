namespace AIInGames.Planning.Runtime
{
    public abstract class PlanningKey<T>
    {
    }

    public abstract class IntKey : PlanningKey<int> { }
    public abstract class FloatKey : PlanningKey<float> { }
    public abstract class StringKey : PlanningKey<string> { }
}
