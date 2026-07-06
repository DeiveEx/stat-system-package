using System.Text;

namespace DeiveEx.StatSystem
{
    /// <summary>
    /// Utility methods for <see cref="StatsContainer{T}"/>
    /// </summary>
    public static class StatContainerExtensions
    {
        /// <summary>
        /// Helper method to add a value to the given stat base value
        /// </summary>
        /// <param name="container">The target container</param>
        /// <param name="statKey">The stat to set the value of</param>
        /// <param name="value">The value to be added</param>
        /// <param name="bypassStatHandler">Should the stat handler be skipped? The stat handler can process the value before setting it</param>
        public static void AddToStat<T>(this StatsContainer<T> container, T statKey, float value, bool bypassStatHandler = false)
        {
            var currentValue = container.GetStatBaseValue(statKey);
            container.SetStat(statKey, currentValue + value, bypassStatHandler);
        }

        /// <summary>
        /// Checks if a stat exists. If it does, set the stat base value, if it doesn't, create the stat with the given value
        /// </summary>
        /// <param name="container">The target container</param>
        /// <param name="statID">The stat to set/create</param>
        /// <param name="value">The value to set the stat to</param>
        public static void SetOrAddStat<T>(this StatsContainer<T> container, T statID, float value)
        {
            if (!container.StatExists(statID))
                container.AddStat(statID, value);
            else
                container.SetStat(statID, value);
        }
        
        /// <summary>
        /// Adds a collection  of stats
        /// </summary>
        /// <param name="container">The target container</param>
        /// <param name="statsCollection">The collection stats to add</param>
        public static void AddStatRange<T>(this StatsContainer<T> container, params (T key, float stat)[] statsCollection)
        {
            foreach (var statInfo in statsCollection)
                container.AddStat(statInfo.key, statInfo.stat);
        }

        /// <summary>
        /// Get a stat current value, if it exists inside the container
        /// </summary>
        /// <param name="container">The target container</param>
        /// <param name="statKey">The stat to get</param>
        /// <param name="value">The current value of the stat</param>
        /// <returns>True if the stat exists in teh container</returns>
        public static bool TryGetStat<T>(this StatsContainer<T> container, T statKey, out float value)
        {
            value = 0;
            
            if(!container.StatExists(statKey))
                return false;
            
            value = container.GetStat(statKey);
            return true;
        }
        
        /// <summary>
        /// Get a stat base value (before modifiers are applied), if it exists inside the container
        /// </summary>
        /// <param name="container">The target container</param>
        /// <param name="statKey">The stat to get</param>
        /// <param name="value">The base value of the stat</param>
        /// <returns>True if the stat exists in teh container</returns>
        public static bool TryGetStatBaseValue<T>(this StatsContainer<T> container, T statKey, out float value)
        {
            value = 0;
            
            if(!container.StatExists(statKey))
                return false;
            
            value = container.GetStatBaseValue(statKey);
            return true;
        }
        
        /// <summary>
        /// Returns a string containing some Debug information about the stats in the given container
        /// </summary>
        /// <param name="container">The target container</param>
        public static string GetDebugInfo<T>(this StatsContainer<T> container)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"= [STATS<{container.Id}>]");
            sb.Append("\n");

            foreach (var id in container.Stats)
            {
                sb.Append($"- {id}: {container.GetStat(id)} (Base: {container.GetStatBaseValue(id)})");
                sb.Append("\n");

                foreach (var modifier in container.GetStatModifiers(id))
                {
                    sb.Append($"  - [{modifier.ID}] {modifier.GetDebugInfo()}");
                    sb.Append("\n");
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Returns a string describing a modifier for Debug purposes
        /// </summary>
        /// <param name="modifier">The target modifier</param>
        public static string GetDebugInfo(this StatModifier modifier)
        {
            return modifier is ValueModifier valueModifier
                ? $"{modifier.GetType().Name}: {valueModifier.Value}"
                : modifier.GetType().Name;
        }
    }
}
