using System;

namespace DeiveEx.StatSystem
{
	public enum OperationType
	{
		Additive,
		Multiplicative,
		Override,
		Custom
	}

	[Serializable]
	public class StatModifier
	{
		public string id;
		public OperationType operationType;
		public float magnitude;
		public int priority; //Only used when the operation type is set to "Override"
		public CustomCalculationDelegate customCalculation; //Only used when operation type is set to "Custom"

		public delegate float CustomCalculationDelegate(float baseValue, float currentValue);

		public StatModifier(string id, OperationType operationType, float magnitude, int priority = 0, CustomCalculationDelegate customCalculation = null)
		{
			this.id = id;
			this.operationType = operationType;
			this.magnitude = magnitude;
			this.priority = priority;

			if (operationType == OperationType.Custom && customCalculation == null)
				throw new NullReferenceException("You need to provide a CustomCalculationDelegate when setting the operation \"Custom\"");
				
			this.customCalculation = customCalculation;
		}
	}
}
