using System.Collections.Generic;

namespace DeiveEx.StatSystem
{
	internal class StatDefinition
	{
		#region Fields

		private readonly List<StatModifier> _modifiers;

		#endregion

		#region Properties

		/// <summary>
		/// The base value for the stat
		/// </summary>
		public float BaseValue { get; set; }

		/// <summary>
		/// The value of the stat after all modifiers are applied
		/// </summary>
		public float CurrentValue { get; set; }

		/// <summary>
		/// All modifiers currently applied to this stat
		/// </summary>
		public IReadOnlyList<StatModifier> Modifiers => _modifiers;

		#endregion

		#region Constructors

		public StatDefinition(float startValue)
		{
			BaseValue = startValue;
			CurrentValue = startValue;
			_modifiers = new();
		}

		#endregion

		#region Public Methods

		public void AddModifier(StatModifier modifier)
		{
			_modifiers.Add(modifier);
		}

		/// <summary>
		/// Returns the first applied modifier with the given ID, or null if there's none
		/// </summary>
		public StatModifier FindModifier(string id)
		{
			for (int i = 0; i < _modifiers.Count; i++)
			{
				if (_modifiers[i].ID == id)
					return _modifiers[i];
			}

			return null;
		}

		/// <summary>
		/// Removes the first occurrence of the given modifier instance
		/// </summary>
		public bool RemoveModifier(StatModifier modifier)
		{
			return _modifiers.Remove(modifier);
		}

		#endregion
	}
}