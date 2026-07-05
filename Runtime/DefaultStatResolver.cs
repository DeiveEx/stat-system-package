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

            //If we have an override, we skip the additive/multiplicative calculations since it makes no sense to calculate anything if it's gonna be overriden
            var activeOverride = GetActiveOverride(modifiers);
            var currentValue = activeOverride?.Value ?? CalculateModifiedValue(baseValue, modifiers);

            return ApplyCustomCalculations(currentValue, baseValue, modifiers, activeOverride != null);
        }

        /// <summary>
        /// Returns the override that should be applied: the one with the highest priority, or between overrides
        /// with the same priority, the last applied. Returns null if there's no override modifier.
        /// </summary>
        private static OverrideModifier GetActiveOverride(IReadOnlyList<StatModifier> modifiers)
        {
            OverrideModifier activeOverride = null;

            for (int i = 0; i < modifiers.Count; i++)
            {
                if (modifiers[i] is OverrideModifier overrideModifier && overrideModifier.Priority >= (activeOverride?.Priority ?? int.MinValue))
                    activeOverride = overrideModifier;
            }

            return activeOverride;
        }

        /// <summary>
        /// Applies all additive modifiers to the base value, then all multiplicative modifiers over that result
        /// </summary>
        private static float CalculateModifiedValue(float baseValue, IReadOnlyList<StatModifier> modifiers)
        {
            float additive = 0;
            float multiplicative = 0;

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
                }
            }

            var currentValue = baseValue + additive;
            return currentValue + (currentValue * multiplicative);
        }

        /// <summary>
        /// Applies all custom calculations, in the order they were applied.
        /// While an override is active, only the ones that explicitly opted in are applied.
        /// </summary>
        private static float ApplyCustomCalculations(float currentValue, float baseValue, IReadOnlyList<StatModifier> modifiers, bool hasOverride)
        {
            for (int i = 0; i < modifiers.Count; i++)
            {
                if (modifiers[i] is CustomCalculationModifier customModifier && (!hasOverride || customModifier.ApplyOnOverride))
                    currentValue = customModifier.Calculation(baseValue, currentValue);
            }

            return currentValue;
        }
    }
}