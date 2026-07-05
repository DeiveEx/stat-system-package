using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeiveEx.StatSystem
{
	public class StatsContainer<T>
	{
		#region Fields

		public const string DEFAULT_CONTAINER_ID = "DefaultStats";
		
		private readonly string _id;
		private readonly Dictionary<T, StatDefinition> _stats = new();
		private readonly Dictionary<T, StatChangeHandlerDelegate> _baseValueHandlers = new();
		private readonly IStatCurrentValueResolver<T> _currentValueResolver;
		
		#endregion
		
		#region Properties

		public string Id => _id;
		public IReadOnlyCollection<T> Stats => _stats.Keys;
		
		#endregion

		#region Events & Delegates
		
		public delegate float StatChangeHandlerDelegate(T targetStat, float targetValue, StatsContainer<T> statsContainer);

		public event EventHandler<T> StatAdded;
		public event EventHandler<T> StatRemoved;
		public event EventHandler<T> ModifierAdded;
		public event EventHandler<T> ModifierRemoved;
		public event EventHandler<StatChangedEventArgs<T>> StatValueChanged;
		public event EventHandler<StatChangedEventArgs<T>> StatBaseValueChanged;

		#endregion
		
		#region Constructors

		public StatsContainer(IStatCurrentValueResolver<T> statCurrentValueResolver, string id = DEFAULT_CONTAINER_ID)
		{
			if (string.IsNullOrEmpty(id))
				id = DEFAULT_CONTAINER_ID;
			
			_id = id;
			_currentValueResolver = statCurrentValueResolver;
		}
		
		#endregion

		#region Public Methods
		
		/// <summary>
		/// Check if a stat exists in this container
		/// </summary>
		/// <param name="statKey">The stat to search for</param>
		/// <returns>True if if it exists</returns>
		public bool StatExists(T statKey)
		{
			return _stats.ContainsKey(statKey);
		}

		/// <summary>
		/// Adds a Stat to the stat container so you can get/set its value
		/// </summary>
		/// <param name="key">The key to identify this stat</param>
		/// <param name="initialValue">The initial value of the stat</param>
		/// <exception cref="InvalidOperationException">Throws if this stat already exists</exception>
		public void AddStat(T key, float initialValue = 0)
		{
			//We don't wanna add repeated states
			if (StatExists(key))
				throw new InvalidOperationException($"A stat with the name [{key}] was already added to the stat list");

			var newStat = new StatDefinition(initialValue);
			_stats.Add(key, newStat);
			StatAdded?.Invoke(this, key);
		}

		/// <summary>
		/// Removes a stat from this container, along with any registered base value handler
		/// </summary>
		/// <param name="statKey">The stat to remove</param>
		/// <returns>True if the stat existed and was removed</returns>
		public bool RemoveStat(T statKey)
		{
			if (!_stats.Remove(statKey))
				return false;

			_baseValueHandlers.Remove(statKey);
			StatRemoved?.Invoke(this, statKey);
			return true;
		}

		/// <summary>
		/// Gets a stat current value, AFTER applying all modifiers
		/// </summary>
		/// <param name="statKey">The stat to search for</param>
		/// <returns>The stat current value</returns>
		public float GetStat(T statKey)
		{
			return GetStatDefinition(statKey).CurrentValue;
		}
		
		/// <summary>
		/// Gets a stat base value, BEFORE applying all modifiers
		/// </summary>
		/// <param name="statKey">The stat to search for</param>
		/// <returns>The stat base value</returns>
		public float GetStatBaseValue(T statKey)
		{
			return GetStatDefinition(statKey).BaseValue;
		}

		/// <summary>
		/// Set the stat base value and recalculates the current value if any modifier is applied.
		/// </summary>
		/// <param name="statKey">The stat to set the value of</param>
		/// <param name="value">The value to be set</param>
		/// <param name="bypassStatHandler">Should the stat handler be skipped? The stat handler can process the value before setting it</param>
		public void SetStat(T statKey, float value, bool bypassStatHandler = false)
		{
			StatDefinition stat = GetStatDefinition(statKey);
			float oldValue = stat.BaseValue;
			float newValue = value;
			
			//Check if we have a custom handler for this stat, and call it
			if (!bypassStatHandler && _baseValueHandlers.TryGetValue(statKey, out StatChangeHandlerDelegate handler))
				newValue = handler(statKey, value, this);

			//If we don't, just override the value
			stat.BaseValue = newValue;
			ResolveStatCurrentValue(statKey, stat);

			if (!Mathf.Approximately(oldValue, newValue))
			{
				StatBaseValueChanged?.Invoke(this, new StatChangedEventArgs<T>()
				{
					Stat = statKey,
					OldValue = oldValue,
					NewValue = newValue,
				});
			}
		}

		/// <summary>
		/// Apply a modifier to the state
		/// </summary>
		/// <param name="statKey">The stat to receive the modifier</param>
		/// <param name="modifier">The modifier to be applied</param>
		public void ApplyModifier(T statKey, StatModifier modifier)
		{
			var stat = GetStatDefinition(statKey);
			stat.AddModifier(modifier);
			ResolveStatCurrentValue(statKey, stat);
			ModifierAdded?.Invoke(this, statKey);
		}

		/// <summary>
		/// Removes a modifier from a stat
		/// </summary>
		/// <param name="statKey">The stat to remove the modifier from</param>
		/// <param name="id">The ID of the modifier</param>
		/// <param name="removeAll">Should all modifiers with the same ID be removed? If false, only the first applied one will be removed</param>
		public bool RemoveModifier(T statKey, string id, bool removeAll = false)
		{
			var stat = GetStatDefinition(statKey);
			
			if (!stat.RemoveModifier(id, removeAll)) 
				return false;
			
			ResolveStatCurrentValue(statKey, stat);
			ModifierRemoved?.Invoke(this, statKey);
			return true;
		}
		
		public IReadOnlyList<StatModifier> GetStatModifiers(T statKey)
		{
			return GetStatDefinition(statKey).Modifiers;
		}

		/// <summary>
		/// Register a handler that can modify the final base value <b>BEFORE</b> the value is applied to the Stat.
		/// <para>One good use of this is for clamping.</para>
		/// </summary>
		/// <param name="statKey">The target stat</param>
		/// <param name="handler">The handler object that will modify the stat value</param>
		public void RegisterBaseValueHandler(T statKey, StatChangeHandlerDelegate handler)
		{
			var stat = GetStatDefinition(statKey);
			
			if (!_baseValueHandlers.TryAdd(statKey, handler))
				throw new InvalidOperationException($"A handler for the base value of the stat with key [{statKey}] already exists. Only one handler per stat is allowed.");
			
			//Apply the handler
			SetStat(statKey, stat.BaseValue);
		}

		/// <summary>
		/// Unregister a Stat Base Value handler
		/// </summary>
		/// <param name="targetStat">The key of the Handler to remove</param>
		public void UnregisterBaseValueHandler(T targetStat)
		{
			_baseValueHandlers.Remove(targetStat);
		}
		
		#endregion
		
		#region Private Methods

		private void ResolveStatCurrentValue(T statKey, StatDefinition stat)
		{
			var oldValue = stat.CurrentValue;
			stat.CurrentValue = _currentValueResolver.ResolveCurrentValue(statKey, this, stat.Modifiers);

			if (Mathf.Approximately(oldValue, stat.CurrentValue))
				return;

			StatValueChanged?.Invoke(this, new StatChangedEventArgs<T>()
			{
				Stat = statKey,
				OldValue = oldValue,
				NewValue = stat.CurrentValue,
			});
		}
		
		private StatDefinition GetStatDefinition(T statKey)
		{
			if (!StatExists(statKey))
				throw new KeyNotFoundException($"Stat container [{_id}] does not have a stat with the key [{statKey}]");

			return _stats[statKey];
		}

		#endregion
	}
}
