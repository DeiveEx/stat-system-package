namespace DeiveEx.StatSystem
{
    public class OverrideModifier : ValueModifier
    {
        public readonly int Priority;

        public OverrideModifier(string id, float value, int priority = 0) : base(id, value)
        {
            Priority = priority;
        }
    }
}