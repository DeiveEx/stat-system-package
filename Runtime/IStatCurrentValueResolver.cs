using System.Collections.Generic;

namespace DeiveEx.StatSystem
{
    public interface IStatCurrentValueResolver<T>
    {
        float ResolveCurrentValue(T stat, StatsContainer<T> container, IReadOnlyList<StatModifier> modifiers);
    }
}