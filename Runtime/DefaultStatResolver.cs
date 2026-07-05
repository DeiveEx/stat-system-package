using System.Collections.Generic;

namespace DeiveEx.StatSystem
{
    public class DefaultStatResolver<T> : IStatCurrentValueResolver<T>
    {
        public float ResolveCurrentValue(T stat, StatsContainer<T> container, IReadOnlyList<StatModifier> modifiers)
        {
            var baseValue = container.GetStatBaseValue(stat);

            if (modifiers.Count == 0)
                return baseValue;

            float additive = 0;
            float multiplicative = 0;
            OverrideModifier activeOverride = null;

            for (int i = 0; i < modifiers.Count; i++)
            {
                switch (modifiers[i])
                {
                    case AdditiveModifier additiveModifier:
                        additive += additiveModifier.Value;
                        break;
                    case MultiplicativeModifier multiplicativeModifier:
                        multiplicative += multiplicativeModifier.Value;
                        break;
                    case OverrideModifier overrideModifier:
                        //The highest priority wins. Between overrides with the same priority, the last applied wins
                        if (activeOverride == null || overrideModifier.Priority >= activeOverride.Priority)
                            activeOverride = overrideModifier;
                        break;
                }
            }

            float currentValue;

            if (activeOverride != null)
            {
                //If we have an override, we skip the additive/multiplicative calculations since it makes no sense to calculate anything if it's gonna be overriden
                currentValue = activeOverride.Value;
            }
            else
            {
                //First we apply all additive modifiers, then all multiplicative modifiers are applied over that result
                currentValue = baseValue + additive;
                currentValue += currentValue * multiplicative;
            }

            //Custom calculations are applied after everything else, in the order they were added.
            //While an override is active, only the ones that explicitly opted in are applied
            for (int i = 0; i < modifiers.Count; i++)
            {
                if (modifiers[i] is not CustomCalculationModifier customModifier)
                    continue;

                if (activeOverride != null && !customModifier.ApplyOnOverride)
                    continue;

                currentValue = customModifier.Calculation(baseValue, currentValue);
            }

            return currentValue;
        }
    }
}