using System;

namespace DeiveEx.StatSystem
{
    public class StatChangedEventArgs<T> : EventArgs
    {
        public T Stat;
        public float OldValue;
        public float NewValue;
    }
}