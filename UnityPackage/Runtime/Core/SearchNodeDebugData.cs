#if DEBUG_PLAN
namespace AIInGames.Planning.Runtime
{
    public struct SearchNodeDebugData
    {
        public string ActionName;
        public float GCost;
        public float HCost;
        public float FCost;
    }
}
#endif
