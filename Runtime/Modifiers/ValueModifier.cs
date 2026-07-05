namespace DeiveEx.StatSystem
{
    public abstract class ValueModifier : StatModifier
    {
        public float Value { get; }
		
        protected ValueModifier(string id, float value) : base(id)
        {
            Value = value;
        }
    }
}