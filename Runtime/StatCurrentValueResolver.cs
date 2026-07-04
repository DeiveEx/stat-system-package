using System.Collections.Generic;
using System.Linq;

namespace DeiveEx.StatSystem
{
    public class StatCurrentValueResolver<T> : IStatCurrentValueResolver<T>
    {
        public float ResolveCurrentValue(T stat, StatsContainer<T> container, ICollection<StatModifier> modifiers)
        {
            var baseValue = container.GetStatBaseValue(stat);
            var currentValue = baseValue;

            if (modifiers.Count == 0)
                return currentValue;

            //First, we check if we have any override modifiers. If we do, we skip the other calculations since it makes no sense to calculate anything else if it's gonna be overriden
            if (CalculateOverride(modifiers, out var overridenValue)) 
                return overridenValue;
			
            //If no override modifier was found, we first apply all additive modifiers
            float additive = CalculateAdditive(modifiers);

            //After that, we apply all multiplicative modifiers
            float multiplicative = CalculateMultiplicative(modifiers);

            //Apply the calculation
            currentValue = baseValue + additive;
            currentValue += (currentValue * multiplicative);

            //If there are any custom calculations, we apply after everything else
            currentValue = CalculateCustom(modifiers, currentValue, baseValue);
			
            return currentValue;
        }
		
        private float CalculateAdditive(ICollection<StatModifier> modifiers)
        {
            float totalValue = 0;

            var additiveModifiers = modifiers.OfType<AdditiveModifier>();

            foreach (var modifier in additiveModifiers)
                totalValue += modifier.Value;

            return totalValue;
        }

        private float CalculateMultiplicative(ICollection<StatModifier> modifiers)
        {
            float totalValue = 0;

            var multiplicativeModifiers = modifiers.OfType<MultiplicativeModifier>();

            foreach (var modifier in multiplicativeModifiers)
                totalValue += modifier.Value;

            return totalValue;
        }

        private bool CalculateOverride(ICollection<StatModifier> modifiers, out float overridenValue)
        {
            var highestPriorityOverride = modifiers.OfType<OverrideModifier>()
                .OrderByDescending(x => x.Priority)
                .FirstOrDefault();

            if (highestPriorityOverride == null)
            {
                overridenValue = 0;
                return false;
            }
            
            //If we do, we get the one with the highest priority and return that
            overridenValue = highestPriorityOverride.Value;
            return true;
        }

        private float CalculateCustom(ICollection<StatModifier> modifiers, float currentValue, float baseValue)
        {
            float value = currentValue;
            var customModifiers = modifiers.OfType<CustomCalculationModifier>();

            foreach (var modifier in customModifiers)
                value = modifier.Calculation(baseValue, value);

            return value;
        }
    }
}