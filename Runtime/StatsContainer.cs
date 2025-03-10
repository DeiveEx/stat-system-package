using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DeiveEx.StatSystem
{
	public class StatsContainer
	{
		#region Fields

		public const string DEFAULT_CONTAINER_ID = "DefaultStats";
		
		private string _id;
		private Dictionary<string, Stat> _stats = new();
		private Dictionary<string, StatBaseValueChangeHandler> _statBaseValueChangeHandlers = new();
		private Dictionary<string, StatBaseValueChangeHandler> _statCurrentValueChangeHandlers = new();
		
		#endregion
		
		#region Properties

		public string Id => _id;
		public IEnumerable<Stat> Stats => _stats.Values;
		
		#endregion

		#region Events & Delegates

		public event EventHandler<StatChangedEventArgs> onStatBaseValueChanged;
		public event EventHandler<StatChangedEventArgs> onModifierAdded;
		public event EventHandler<StatChangedEventArgs> onModifierRemoved;

		#endregion
		
		#region Public Methods

		public StatsContainer(string id = null)
		{
			if (string.IsNullOrEmpty(id))
				id = DEFAULT_CONTAINER_ID;
			
			_id = id;
		}

		/// <summary>
		/// Adds a Stat to the stat container so you can get/set its value
		/// </summary>
		/// <param name="newStat">The stat to be added</param>
		/// <exception cref="InvalidOperationException">Throws if this stat already exists</exception>
		public void AddStat(Stat newStat)
		{
			//We don't wanna add repeated states
			if (StatExists(newStat.Name))
				throw new InvalidOperationException($"A stat with the name [{newStat.Name}] was already added to the stat list");

			newStat.onBaseValueChanged += Stat_OnBaseValueChanged;
			newStat.onModifierAdded += Stat_OnModifierAdded;
			newStat.onModifierRemoved += Stat_OnModifierRemoved;

			_stats.Add(newStat.Name, newStat);
		}

		/// <summary>
		/// Checks if a stat exists. If it does, set the stat base value, if it doesn't, create the stat with the given value
		/// </summary>
		/// <param name="statID">The stat to set/create</param>
		/// <param name="value">The value to set the stat to</param>
		public void SetOrAddStat(string statID, float value)
		{
			if (!StatExists(statID))
				AddStat(new Stat(statID, value));
			
			SetStatBaseValue(statID, value);
		}
		
		/// <summary>
		/// Adds a collection  of stats
		/// </summary>
		/// <param name="statsCollection">The collection stats to add</param>
		public void AddStatRange(IEnumerable<Stat> statsCollection)
		{
			foreach (var stat in statsCollection)
			{
				AddStat(stat);
			}
		}

		/// <summary>
		/// Check if a stat exists in this container
		/// </summary>
		/// <param name="statKey">The stat to sear for</param>
		/// <returns>True if if it exists</returns>
		public bool StatExists(string statKey)
		{
			return _stats.ContainsKey(statKey);
		}

		/// <summary>
		/// Get the base value of the stat. The base value is the value before any modifiers are applied.
		/// <para>
		/// You should only call this in specific situations. Call <see cref="GetStatCurrentValue"/> for more common uses (like checking the character attack or HP).
		/// </para>
		/// </summary>
		/// <param name="statKey">The stat to get the base value from</param>
		/// <returns>The Stat base value</returns>
		public float GetStatBaseValue(string statKey)
		{
			return GetStat(statKey).BaseValue;
		}

		/// <summary>
		/// Get the current value of the stat. The current value is the value after all modifiers were applied.
		/// </summary>
		/// <param name="statKey">The stat to get the current value from</param>
		/// <returns>The stat current value</returns>
		public float GetStatCurrentValue(string statKey)
		{
			Stat stat = GetStat(statKey);
			return stat.CurrentValue;
		}

		/// <summary>
		/// Set the stat base value.
		/// </summary>
		/// <param name="statKey">The stat to set the value of</param>
		/// <param name="value">The value to be set</param>
		/// <param name="bypassStatHandler">Should the stat handler be skipped? The stat handler can process the value before setting it</param>
		public void SetStatBaseValue(string statKey, float value, bool bypassStatHandler = false)
		{
			Stat stat = GetStat(statKey);

			//Check if we have a custom handler for this stat
			if (!bypassStatHandler && _statBaseValueChangeHandlers.TryGetValue(statKey, out StatBaseValueChangeHandler handler))
			{
				//If we do, call the handler
				stat.BaseValue = handler.HandleValueChange(stat, value, this);
				return;
			}

			//If we don't, just override the value
			stat.BaseValue = value;
		}

		/// <summary>
		/// Helper method to add a value to the current stat base value
		/// </summary>
		/// <param name="statKey">The stat to set teh value of</param>
		/// <param name="value">The value to be added</param>
		/// <param name="bypassStatHandler">Should the stat handler be skipped? The stat handler can process the value before setting it</param>
		public void AddToStatBaseValue(string statKey, float value, bool bypassStatHandler = false)
		{
			var currentValue = GetStatCurrentValue(statKey);
			SetStatBaseValue(statKey, currentValue + value, bypassStatHandler);
		}

		/// <summary>
		/// Apply a modifier to the state
		/// </summary>
		/// <param name="statKey">The stat to receive the modifier</param>
		/// <param name="modifier">The modifier to be applied</param>
		public void ApplyModifier(string statKey, StatModifier modifier)
		{
			if (!StatExists(statKey))
			{
				Debug.LogWarning($"Trying to apply a modifier to stat [{statKey}], which doesnt exist in the target {nameof(StatsContainer)}");
				return;
			}
			
			_stats[statKey].AddModifier(modifier);
		}

		/// <summary>
		/// Removes a modifier from a stat
		/// </summary>
		/// <param name="statKey">The stat to remove the modifier from</param>
		/// <param name="id">The ID of the modifier</param>
		public void RemoveModifier(string statKey, string id)
		{
			if (!StatExists(statKey))
			{
				Debug.LogWarning($"Trying to remove a modifier from stat [{statKey}], which doesnt exist in the target {nameof(StatsContainer)}");
				return;
			}
			
			_stats[statKey].RemoveModifier(id);
		}

		/// <summary>
		/// Register a handler that can modify the final base value <b>BEFORE</b> the value is applied to the Stat.
		/// <para>One good use of this is for clamping.</para>
		/// </summary>
		/// <param name="handler">The handler object that will modify the stat value</param>
		public void RegisterBaseValueHandler(StatBaseValueChangeHandler handler)
		{
			if (_statBaseValueChangeHandlers.ContainsKey(handler.TargetStat))
				throw new InvalidOperationException($"A handler for the base value of the stat with ID [{handler.TargetStat}] already exists. Only one handler per stat is allowed.");

			_statBaseValueChangeHandlers.Add(handler.TargetStat, handler);
		}

		/// <summary>
		/// Unregister a Stat Base Value handler
		/// </summary>
		/// <param name="targetStat">The id of the Handler to remove</param>
		public void UnregisterBaseValueHandler(string targetStat)
		{
			if (!_statBaseValueChangeHandlers.ContainsKey(targetStat))
				return;

			_statBaseValueChangeHandlers.Remove(targetStat);
		}

		/// <summary>
		/// Register a handler that can modify the stat value <b>AFTER</b> all modifiers are applied to the base stat value.
		/// <para>One good use of this is for clamping.</para>
		/// </summary>
		/// <param name="handler">The handler object that will modify the stat value</param>
		public void RegisterCurrentValueHandler(StatBaseValueChangeHandler handler)
		{
			if (_statCurrentValueChangeHandlers.ContainsKey(handler.TargetStat))
				throw new InvalidOperationException($"A handler for the stat with ID [{handler.TargetStat}] already exists. Only one handler per stat is allowed.");

			_statCurrentValueChangeHandlers.Add(handler.TargetStat, handler);
		}

		/// <summary>
		/// Unregister a Stat Current Value handler
		/// </summary>
		/// <param name="targetStat">The id of the Handler to remove</param>
		public void UnregisterCurrentValueHandler(string targetStat)
		{
			if (!_statCurrentValueChangeHandlers.ContainsKey(targetStat))
				return;

			_statCurrentValueChangeHandlers.Remove(targetStat);
		}

		public string GetDebugInfo()
		{
			StringBuilder sb = new StringBuilder($"= [STATS<{_id}>]");
			sb.Append("\n");

			foreach (var stat in _stats.Values)
			{
				sb.Append($"- {stat.Name}: {stat.CurrentValue} (Base: {stat.BaseValue})").Append("\n");
			}

			return sb.ToString();
		}

		public StatsContainerState GetState()
		{
			var stats = new List<StatWrapper>();

			foreach (var stat in Stats)
			{
				stats.Add(new StatWrapper()
				{
					StatName = stat.Name,
					BaseValue = stat.BaseValue,
				});
			}

			return new StatsContainerState()
			{
				StatCount = stats.Count,
				Stats = stats.ToArray(),
			};
		}

		public void ApplyState(StatsContainerState state)
		{
			foreach (var statWrapper in state.Stats)
			{
				SetOrAddStat(statWrapper.StatName, statWrapper.BaseValue);
			}
		}
		
		#endregion
		
		#region Private Methods

		private Stat GetStat(string statKey)
		{
			if (!StatExists(statKey))
				throw new NullReferenceException($"This container does not have a stat with the statKey [{statKey}]");

			return _stats[statKey];
		}
		
		private void Stat_OnBaseValueChanged(object sender, StatChangedEventArgs e)
		{
			onStatBaseValueChanged?.Invoke(this, e);
		}
		
		private void Stat_OnModifierAdded(object sender, StatChangedEventArgs e)
		{
			onModifierAdded?.Invoke(this, e);
		}

		private void Stat_OnModifierRemoved(object sender, StatChangedEventArgs e)
		{
			onModifierRemoved?.Invoke(this, e);
		}
		
		#endregion
	}
}
