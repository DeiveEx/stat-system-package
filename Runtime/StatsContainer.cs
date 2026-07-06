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
		public event EventHandler<ModifierChangedEventArgs<T>> ModifierAdded;
		public event EventHandler<ModifierChangedEventArgs<T>> ModifierRemoved;
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
		/// <returns>True if it exists</returns>
		public bool HasStat(T statKey)
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
			if (HasStat(key))
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
		/// Apply a modifier to the stat
		/// </summary>
		/// <param name="statKey">The stat to receive the modifier</param>
		/// <param name="modifier">The modifier to be applied</param>
		/// <returns>A handle that removes this exact modifier instance when disposed. Disposing it more than once,
		/// or after the modifier/stat was already removed, is a safe no-op. Ignoring it is also fine.</returns>
		public IDisposable ApplyModifier(T statKey, StatModifier modifier)
		{
			var stat = GetStatDefinition(statKey);
			stat.AddModifier(modifier);
			ResolveStatCurrentValue(statKey, stat);
			ModifierAdded?.Invoke(this, new ModifierChangedEventArgs<T>()
			{
				Stat = statKey,
				Modifier = modifier,
			});

			return new ModifierHandle(this, statKey, modifier);
		}

		/// <summary>
		/// Removes a modifier from a stat by its ID
		/// </summary>
		/// <param name="statKey">The stat to remove the modifier from</param>
		/// <param name="id">The ID of the modifier</param>
		/// <param name="removeAll">Should all modifiers with the same ID be removed? If false, only the first applied one will be removed</param>
		public bool RemoveModifier(T statKey, string id, bool removeAll = false)
		{
			var stat = GetStatDefinition(statKey);
			bool removedAny = false;

			do
			{
				var modifier = stat.FindModifier(id);

				if (modifier == null)
					break;

				stat.RemoveModifier(modifier);
				ResolveStatCurrentValue(statKey, stat);
				ModifierRemoved?.Invoke(this, new ModifierChangedEventArgs<T>()
				{
					Stat = statKey,
					Modifier = modifier,
				});

				removedAny = true;
			} while (removeAll);

			return removedAny;
		}

		/// <summary>
		/// Removes a specific modifier instance from a stat.
		/// <para>Unlike the ID overload, this doesn't throw if the stat doesn't exist, so handles can be safely disposed after a stat was removed.</para>
		/// </summary>
		/// <param name="statKey">The stat to remove the modifier from</param>
		/// <param name="modifier">The modifier instance to remove</param>
		/// <returns>True if the modifier was applied to the stat and was removed</returns>
		public bool RemoveModifier(T statKey, StatModifier modifier)
		{
			if (!_stats.TryGetValue(statKey, out var stat) || !stat.RemoveModifier(modifier))
				return false;

			ResolveStatCurrentValue(statKey, stat);
			ModifierRemoved?.Invoke(this, new ModifierChangedEventArgs<T>()
			{
				Stat = statKey,
				Modifier = modifier,
			});

			return true;
		}

		public IReadOnlyList<StatModifier> GetStatModifiers(T statKey)
		{
			return GetStatDefinition(statKey).Modifiers;
		}

		/// <summary>
		/// Checks if a stat has at least one modifier with the given ID applied
		/// </summary>
		/// <param name="statKey">The stat to check</param>
		/// <param name="id">The ID of the modifier</param>
		public bool HasModifier(T statKey, string id)
		{
			return GetStatDefinition(statKey).FindModifier(id) != null;
		}

		/// <summary>
		/// Re-resolves the current value of a stat, firing <see cref="StatValueChanged"/> if it changed.
		/// <para>Useful when a modifier reads OTHER stats: the container can't know about that dependency, so call this to refresh the value after the other stat changes.</para>
		/// </summary>
		/// <param name="statKey">The stat to recalculate</param>
		public void RecalculateStat(T statKey)
		{
			ResolveStatCurrentValue(statKey, GetStatDefinition(statKey));
		}

		/// <summary>
		/// Re-resolves the current value of all stats in this container
		/// </summary>
		public void RecalculateAll()
		{
			foreach (var pair in _stats)
				ResolveStatCurrentValue(pair.Key, pair.Value);
		}

		/// <summary>
		/// Register a handler that can modify the final base value <b>BEFORE</b> the value is applied to the Stat.
		/// <para>One good use of this is for clamping.</para>
		/// <para>Only one handler is allowed per stat: registering a new one replaces the previous handler.</para>
		/// </summary>
		/// <param name="statKey">The target stat</param>
		/// <param name="handler">The handler object that will modify the stat value</param>
		public void RegisterBaseValueHandler(T statKey, StatChangeHandlerDelegate handler)
		{
			if (handler == null)
				throw new ArgumentNullException(nameof(handler));

			var stat = GetStatDefinition(statKey);
			_baseValueHandlers[statKey] = handler;

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

		/// <summary>
		/// Returns a copy of all stats and their current base values, e.g. for saving.
		/// <para>Modifiers are not included, since they're live objects: re-applying them is the game's responsibility.</para>
		/// </summary>
		public Dictionary<T, float> GetBaseValueSnapshot()
		{
			var snapshot = new Dictionary<T, float>(_stats.Count);

			foreach (var pair in _stats)
				snapshot[pair.Key] = pair.Value.BaseValue;

			return snapshot;
		}

		/// <summary>
		/// Applies a snapshot created by <see cref="GetBaseValueSnapshot"/>, e.g. when loading.
		/// <para>Stats that don't exist yet are created. Base value handlers are bypassed, since the snapshot values were already processed when they were set.</para>
		/// </summary>
		/// <param name="snapshot">The snapshot to apply</param>
		public void ApplySnapshot(IReadOnlyDictionary<T, float> snapshot)
		{
			foreach (var pair in snapshot)
			{
				if (HasStat(pair.Key))
					SetStat(pair.Key, pair.Value, bypassStatHandler: true);
				else
					AddStat(pair.Key, pair.Value);
			}
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
			if (!_stats.TryGetValue(statKey, out var stat))
				throw new KeyNotFoundException($"Stat container [{_id}] does not have a stat with the key [{statKey}]");

			return stat;
		}

		#endregion

		#region Nested Types

		private class ModifierHandle : IDisposable
		{
			private StatsContainer<T> _container;
			private readonly T _statKey;
			private readonly StatModifier _modifier;

			public ModifierHandle(StatsContainer<T> container, T statKey, StatModifier modifier)
			{
				_container = container;
				_statKey = statKey;
				_modifier = modifier;
			}

			public void Dispose()
			{
				_container?.RemoveModifier(_statKey, _modifier);
				_container = null;
			}
		}

		#endregion
	}
}
