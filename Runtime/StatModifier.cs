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
		public string ID;
		public OperationType OperationType;
		public float Magnitude;
		public int Priority; //Only used when the operation type is set to "Override"
		public CustomCalculationDelegate CustomCalculation; //Only used when operation type is set to "Custom"

		public delegate float CustomCalculationDelegate(float baseValue, float currentValue);

		public StatModifier(string id, OperationType operationType, float magnitude, int priority = 0, CustomCalculationDelegate customCalculation = null)
		{
			this.ID = id;
			this.OperationType = operationType;
			this.Magnitude = magnitude;
			this.Priority = priority;

			if (operationType == OperationType.Custom && customCalculation == null)
				throw new NullReferenceException("You need to provide a CustomCalculationDelegate when setting the operation \"Custom\"");
				
			this.CustomCalculation = customCalculation;
		}
	}
}
