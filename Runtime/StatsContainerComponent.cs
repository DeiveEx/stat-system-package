using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DeiveEx.StatSystem
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public class StatsContainerComponent : MonoBehaviour
    {
        #region Fields
        
        private Dictionary<string, StatsContainer> _statsContainers = new();

        #endregion
        
        #region Properties

        public IEnumerable<string> ContainerIds => _statsContainers.Keys;

        #endregion
        
        #region Events & Delegates
        
        public event EventHandler<StatChangedEventArgs> onStatBaseValueChanged;
        public event EventHandler<StatChangedEventArgs> onModifierAdded;
        public event EventHandler<StatChangedEventArgs> onModifierRemoved;
        
        #endregion
        
        #region Public Methods
        public void AddStatsContainer(StatsContainer container)
        {
            if (_statsContainers.ContainsKey(container.Id))
                throw new InvalidOperationException($"A {nameof(StatsContainer)} with ID [{container.Id}] already exists!");

            _statsContainers.Add(container.Id, container);
            container.onStatBaseValueChanged += Container_OnStatBaseValueChanged;
            container.onModifierAdded += Container_OnModifierAdded;
            container.onModifierRemoved += Container_OnModifierRemoved;
        }

        public void RemoveStatsContainer(string containerID)
        {
            if (!_statsContainers.ContainsKey(containerID))
                return;

            var container = _statsContainers[containerID];
            container.onStatBaseValueChanged -= Container_OnStatBaseValueChanged;
            container.onModifierAdded -= Container_OnModifierAdded;
            container.onModifierRemoved -= Container_OnModifierRemoved;
            
            _statsContainers.Remove(containerID);
        }
        
        public StatsContainer GetStatsContainer(string containerId = null)
        {
            if (_statsContainers.Count == 0)
                throw new NullReferenceException("No stats containers were added");

            if (string.IsNullOrEmpty(containerId))
                return _statsContainers.First().Value;

            if (_statsContainers.TryGetValue(containerId, out var container))
                return container;
			
            throw new NullReferenceException($"No StatsContainer with id '{containerId}' was found");
        }

        public StatsContainer GetOrAddStatsContainer(string containerId = null)
        {
            if (string.IsNullOrEmpty(containerId))
                containerId = StatsContainer.DEFAULT_CONTAINER_ID;

            if (_statsContainers.TryGetValue(containerId, out var container))
                return container;

            var newContainer = new StatsContainer(containerId);
            AddStatsContainer(newContainer);

            return newContainer;
        }

        #endregion
        
        #region Private Methods

        private void Container_OnStatBaseValueChanged(object sender, StatChangedEventArgs e)
        {
            onStatBaseValueChanged?.Invoke(this, e);
        }
        
        private void Container_OnModifierRemoved(object sender, StatChangedEventArgs e)
        {
            onModifierAdded?.Invoke(this, e);
        }

        private void Container_OnModifierAdded(object sender, StatChangedEventArgs e)
        {
            onModifierRemoved?.Invoke(this, e);
        }
        
        #endregion
    }
}
