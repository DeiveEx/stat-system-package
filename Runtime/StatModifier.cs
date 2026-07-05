using System;

namespace DeiveEx.StatSystem
{
	[Serializable]
	public abstract class StatModifier
	{
		public string ID { get; }

		protected StatModifier(string id)
		{
			ID = id;
		}
	}
}
