using System.Collections.Generic;
using raidTimeline.Logic.EiJson;
using raidTimeline.Models.Models;

namespace raidTimeline.Logic.Interfaces
{
	internal interface IMechanics
	{
		/// <summary>Elite Insights trigger ids of the encounter(s) this strategy handles.</summary>
		IReadOnlyCollection<int> GetEncounterIds();

		void Parse(EiLog log, PlayerModel playerModel);

		string CreateHtml(RaidModel model);
	}
}
