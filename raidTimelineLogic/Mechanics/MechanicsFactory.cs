using raidTimelineLogic.Mechanics.Strategies;
using System.Collections.Generic;
using System.Linq;

namespace raidTimelineLogic.Mechanics
{
	internal class MechanicsFactory
	{
		private List<IMechanics> _strategies = new List<IMechanics>();

		public MechanicsFactory()
		{
			_strategies.Add(new ValeGuardianMechanics());
			_strategies.Add(new GorsevalMechanics());
			_strategies.Add(new SabethaMechanics());
			_strategies.Add(new SlothasorMechanics());
			_strategies.Add(new TrioMechanics());
			_strategies.Add(new MatthiasMechanics());
		}

		public IMechanics FindStrategy(string iconUrl)
		{
			return _strategies.SingleOrDefault(i => i.GetEncounterIcon() == iconUrl);
		}
	}
}