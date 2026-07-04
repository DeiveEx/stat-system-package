using System.Collections.Generic;
using System.Linq;

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
		public ICollection<StatModifier> Modifiers => _modifiers;
		
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

		public bool RemoveModifier(string id, bool removeAll = false)
		{
			if (removeAll)
				return _modifiers.RemoveAll(x => x.id == id) > 0;
			
			var firstModifier = _modifiers.FirstOrDefault(x => x.id == id);
			
			return firstModifier != null && 
			       _modifiers.Remove(firstModifier);
		}

		#endregion
	}
}