namespace AIInGames.Planning.Runtime
{
    public class PlanningObject
    {
        public string Name { get; }
        public string Type { get; }

        public PlanningObject(string name, string type)
        {
            Name = name;
            Type = type;
        }

        public override string ToString() => $"{Name} ({Type})";
    }
}
