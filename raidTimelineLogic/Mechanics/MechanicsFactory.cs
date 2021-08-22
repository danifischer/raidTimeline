using raidTimelineLogic.Mechanics.Strategies;
using System.Collections.Generic;
using System.Linq;

namespace raidTimelineLogic.Mechanics
{
	internal class MechanicsFactory
	{
		private readonly List<IMechanics> _strategies = new List<IMechanics>();

		public MechanicsFactory()
		{
			// W1
			_strategies.Add(new ValeGuardianMechanics());
			_strategies.Add(new GorsevalMechanics());
			_strategies.Add(new SabethaMechanics());

			// W2
			_strategies.Add(new SlothasorMechanics());
			_strategies.Add(new TrioMechanics());
			_strategies.Add(new MatthiasMechanics());

			// W3
			_strategies.Add(new KeepConstructMechanics());
			_strategies.Add(new XeraMechanics());

			// W4
			_strategies.Add(new CairnMechanics());
			_strategies.Add(new MursaatOverseerMechanics());
			_strategies.Add(new SamarogMechanics());
			_strategies.Add(new DeimosMechanics());

			// W5
			_strategies.Add(new SoullessHorrorMechanics());
			_strategies.Add(new RiverOfSoulsMechanics());
			_strategies.Add(new StatueOfIceMechanics());
			_strategies.Add(new StatueOfDeathMechanics());
			_strategies.Add(new StatueOfDarknessMechanics());
			_strategies.Add(new DhuumMechanics());

			// W6
			_strategies.Add(new ConjuredAmalgamateMechanics());
			_strategies.Add(new TwinLargosMechanics());
			_strategies.Add(new QadimMechanics());

			// W7
			_strategies.Add(new CardinalSabirMechanics());
			_strategies.Add(new CardinalAdinaMechanics());
			_strategies.Add(new QadimThePeerlessMechanics());
		}

		public IMechanics FindStrategy(string iconUrl)
		{
			return _strategies.SingleOrDefault(i => i.GetEncounterIcon() == iconUrl);
		}
	}
}