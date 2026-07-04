namespace DeiveEx.StatSystem
{
    public class CustomCalculationModifier : StatModifier
    {
        public delegate float CustomCalculationDelegate(float baseValue, float currentValue);
		
        public CustomCalculationDelegate Calculation;

        public CustomCalculationModifier(string id, CustomCalculationDelegate calculation) : base(id)
        {
            Calculation = calculation;
        }
    }
}