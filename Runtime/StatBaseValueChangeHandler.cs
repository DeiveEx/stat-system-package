namespace DeiveEx.StatSystem
{	
	public class StatBaseValueChangeHandler
	{
		private StatChangeHandlerDelegate _handler;

		public string TargetStat { get; private set; }

		public delegate float StatChangeHandlerDelegate(Stat targetStat, float targetValue, StatsContainer statsContainer);


		public StatBaseValueChangeHandler(string targetStat, StatChangeHandlerDelegate handler)
		{
			this.TargetStat = targetStat;
			this._handler = handler;
		}

		public float HandleValueChange(Stat targetStat, float targetValue, StatsContainer statsContainer)
		{
			return _handler(targetStat, targetValue, statsContainer);
		}
	}
}
