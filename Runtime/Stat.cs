using System;
using System.Collections.Generic;
using System.Linq;

namespace DeiveEx.StatSystem
{
	public class StatChangedEventArgs : EventArgs
	{
		public string ID;
		public Stat Stat;
		public float OldBaseValue;
		public float NewBaseValue;
	}
	
	public class Stat
	{
		#region Fields

		private readonly string _name;
		private float _baseValue;

		private float _currentValue;
		private List<StatModifier> _modifiers;

		#endregion

		#region Properties

		public string Name => _name;
		/// <summary>
		/// The base value for the stat
		/// </summary>
		public float BaseValue
		{
			get => _baseValue;
			set
			{
				float oldValue = _baseValue;
				_baseValue = value;
				RecalculateCurrentValue();
				
				OnBaseValueChanged?.Invoke(this, new StatChangedEventArgs()
				{
					ID = Name,
					Stat = this,
					OldBaseValue = oldValue,
					NewBaseValue = _baseValue
				});
			}
		}
		/// <summary>
		/// The value of the stat after all modifiers are applied
		/// </summary>
		public float CurrentValue => _currentValue;

		#endregion

		#region Events & Delegates

		public event EventHandler<StatChangedEventArgs> OnBaseValueChanged;
		public event EventHandler<StatChangedEventArgs> OnModifierAdded;
		public event EventHandler<StatChangedEventArgs> OnModifierRemoved;

		#endregion
		
		#region Constructors

		public Stat(string name, float baseValue)
		{
			_name = name;
			_baseValue = baseValue;
			_currentValue = baseValue;
			_modifiers = new();
		}

		#endregion

		#region Public Methods

		public void AddModifier(StatModifier modifier)
		{
			_modifiers.Add(modifier);
			RecalculateCurrentValue();
			OnModifierAdded?.Invoke(this, new StatChangedEventArgs()
			{
				ID = Name,
				Stat = this,
				OldBaseValue = _baseValue,
				NewBaseValue = _baseValue
			});
		}

		public void RemoveModifier(string id, bool removeAll = false)
		{
			var firstModifier = _modifiers.FirstOrDefault(x => x.id == id);

			if (firstModifier != null)
			{
				if (removeAll)
				{
					_modifiers = _modifiers.Where(x => x.id != id).ToList();
				}
				else
				{
					_modifiers.Remove(firstModifier);
				}

				//Only recalculate if we actually removed anything
				RecalculateCurrentValue();
				
				OnModifierRemoved?.Invoke(this, new StatChangedEventArgs()
				{
					ID = Name,
					Stat = this,
					OldBaseValue = _baseValue,
					NewBaseValue = _baseValue
				});
			}
		}

		#endregion

		#region Private Methods

		private void RecalculateCurrentValue()
		{
			_currentValue = _baseValue;

			if (_modifiers.Count == 0)
				return;

			//First we check if we have any override modifiers. If we do, we skip the other calculations since it makes no sense to calculate anything else if it's gonna be overriden
			if (!CalculateOverride())
			{
				//We first apply all additive modifiers
				float additive = CalculateAdditive();

				//After that, we apply all multiplicative modifiers
				float multiplicative = CalculateMultiplicative();

				//Apply the calculation
				_currentValue = _baseValue + additive;
				_currentValue += _currentValue * multiplicative;

				//If there's any custom calculations, we apply after everything else
				_currentValue = CalculateCustom();
			}
		}

		private float CalculateAdditive()
		{
			float totalValue = 0;

			var additiveModifiers = _modifiers.Where(x => x.operationType == OperationType.Additive);

			foreach (var modifier in additiveModifiers)
			{
				totalValue += modifier.magnitude;
			}

			return totalValue;
		}

		private float CalculateMultiplicative()
		{
			float totalValue = 0;

			var multiplicativeModifiers = _modifiers.Where(x => x.operationType == OperationType.Multiplicative);

			foreach (var modifier in multiplicativeModifiers)
			{
				totalValue += modifier.magnitude;
			}

			return totalValue;
		}

		private bool CalculateOverride()
		{
			var overrideModifiers = _modifiers.Where(x => x.operationType == OperationType.Override).ToArray();

			//Check if we have any override modifier
			if (overrideModifiers.Length > 0)
			{
				//If we do, we get the one with the highest priority and return that
				StatModifier highestPriorityOverride = overrideModifiers.Aggregate((x, y) => x.priority > y.priority ? x : y); //"Aggregate" gets 2 items, execute some operation and uses the resulting value as the 1st item in the next operation
				_currentValue = highestPriorityOverride.magnitude;
				return true;
			}

			return false;
		}

		private float CalculateCustom()
		{
			float value = _currentValue;
			var customModifiers = _modifiers.Where(x => x.operationType == OperationType.Custom);

			foreach (var modifier in customModifiers)
			{
				value = modifier.customCalculation(_baseValue, value);
			}

			return value;
		}

		#endregion
	}
}