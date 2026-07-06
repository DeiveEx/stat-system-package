using System;

namespace DeiveEx.StatSystem
{
    public class ModifierChangedEventArgs<T> : EventArgs
    {
        public T Stat;
        public StatModifier Modifier;
    }
}