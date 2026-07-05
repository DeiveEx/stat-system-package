using System;

namespace DeiveEx.StatSystem
{
    public class CustomCalculationModifier : StatModifier
    {
        public delegate float CustomCalculationDelegate(float baseValue, float currentValue);

        public CustomCalculationDelegate Calculation { get; }

        /// <summary>
        /// If true, this calculation is still applied (on top of the override value) when an
        /// <see cref="OverrideModifier"/> is active on the same stat. If false, it's skipped entirely while an override is active.
        /// </summary>
        public bool ApplyOnOverride { get; }

        public CustomCalculationModifier(string id, CustomCalculationDelegate calculation, bool applyOnOverride = false) : base(id)
        {
            Calculation = calculation ?? throw new ArgumentNullException(nameof(calculation));
            ApplyOnOverride = applyOnOverride;
        }
    }
}