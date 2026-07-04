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
            if (CalculateOverride(modifiers, out currentValue)) 
                return currentValue;
			
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

            var additiveModifiers = modifiers.Where(x => x.operationType == OperationType.Additive);

            foreach (var modifier in additiveModifiers)
            {
                totalValue += modifier.magnitude;
            }

            return totalValue;
        }

        private float CalculateMultiplicative(ICollection<StatModifier> modifiers)
        {
            float totalValue = 0;

            var multiplicativeModifiers = modifiers.Where(x => x.operationType == OperationType.Multiplicative);

            foreach (var modifier in multiplicativeModifiers)
            {
                totalValue += modifier.magnitude;
            }

            return totalValue;
        }

        private bool CalculateOverride(ICollection<StatModifier> modifiers, out float currentValue)
        {
            var overrideModifiers = modifiers.Where(x => x.operationType == OperationType.Override).ToArray();

            //Check if we have any override modifier
            if (overrideModifiers.Length == 0)
            {
                currentValue = 0;
                return false;
            }
            
            //If we do, we get the one with the highest priority and return that
            StatModifier highestPriorityOverride = overrideModifiers.Aggregate((x, y) => x.priority > y.priority ? x : y); //"Aggregate" gets 2 items, execute some operation and uses the resulting value as the 1st item in the next operation
            currentValue = highestPriorityOverride.magnitude;
            return true;
        }

        private float CalculateCustom(ICollection<StatModifier> modifiers, float currentValue, float baseValue)
        {
            float value = currentValue;
            var customModifiers = modifiers.Where(x => x.operationType == OperationType.Custom);

            foreach (var modifier in customModifiers)
            {
                value = modifier.customCalculation(baseValue, value);
            }

            return value;
        }
    }
}