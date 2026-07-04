namespace DeiveEx.StatSystem
{
    public abstract class ValueModifier : StatModifier
    {
        public readonly float Value;
		
        protected ValueModifier(string id, float value) : base(id)
        {
            Value = value;
        }
    }
}