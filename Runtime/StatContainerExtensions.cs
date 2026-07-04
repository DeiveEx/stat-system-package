using System.Text;

namespace DeiveEx.StatSystem
{
    public static class StatContainerExtensions
    {
		private static readonly StringBuilder _sb = new ();
        
        /// <summary>
        /// Helper method to add a value to the current stat base value
        /// </summary>
        /// <param name="container">The target container</param>
        /// <param name="statKey">The stat to set the value of</param>
        /// <param name="value">The value to be added</param>
        /// <param name="bypassStatHandler">Should the stat handler be skipped? The stat handler can process the value before setting it</param>
        public static void AddToStat<T>(this StatsContainer<T> container, T statKey, float value, bool bypassStatHandler = false)
        {
            var currentValue = container.GetStat(statKey);
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
        /// Returns a string containing some Debug information about the stats in the given container
        /// </summary>
        /// <param name="container">The target container</param>
        public static string GetDebugInfo<T>(this StatsContainer<T> container)
        {
            _sb.Clear();
            _sb.AppendLine($"= [STATS<{container.Id}>]");
            _sb.Append("\n");

            foreach (var id in container.Stats)
            {
                _sb.Append($"- {id}: {container.GetStat(id)} (Base: {container.GetStatBaseValue(id)})");
                _sb.Append("\n");
            }

            return _sb.ToString();
        }
    }
}
