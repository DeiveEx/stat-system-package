using UnityEngine;

namespace DeiveEx.StatSystem
{
    [DisallowMultipleComponent]
    public abstract class StatsContainerComponent<T> : MonoBehaviour
    {
        private StatsContainer<T> _statsContainer;

        public StatsContainer<T> StatsContainer => _statsContainer;

        private void Awake()
        {
            _statsContainer = new StatsContainer<T>(GetResolver());
        }
        
        protected abstract IStatCurrentValueResolver<T> GetResolver();
    }
}
