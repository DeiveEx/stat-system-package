using UnityEngine;

namespace DeiveEx.StatSystem
{
    [DisallowMultipleComponent]
    public abstract class StatsContainerComponent<T> : MonoBehaviour
    {
        private StatsContainer<T> _statsContainer;

        public StatsContainer<T> StatsContainer
        {
            get
            {
                _statsContainer ??= new StatsContainer<T>(GetResolver());
                return _statsContainer;
            }
        }
        
        protected abstract IStatCurrentValueResolver<T> GetResolver();
    }
}
