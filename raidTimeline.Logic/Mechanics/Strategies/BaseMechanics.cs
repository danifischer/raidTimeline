using System.Collections.Generic;
using System.Linq;
using raidTimeline.Logic.EiJson;
using raidTimeline.Logic.Interfaces;
using raidTimeline.Models.Models;

namespace raidTimeline.Logic.Mechanics.Strategies
{
	internal abstract class BaseMechanics : IMechanics
	{
		internal IReadOnlyCollection<int> EncounterIds = new List<int>();

		public abstract string CreateHtml(RaidModel model);

		public IReadOnlyCollection<int> GetEncounterIds()
		{
			return EncounterIds;
		}

		public abstract void Parse(EiLog log, PlayerModel playerModel);

		/// <summary>
		/// Counts for every mechanic of the log how often it was triggered on the player
		/// and stores it under the short name of the mechanic (e.g. "Donut In").
		/// </summary>
		internal static void PrepareParsing(EiLog log, PlayerModel playerModel)
		{
			var characterName = log.Players[playerModel.Index].Name;

			foreach (var mechanic in log.Mechanics)
			{
				var count = mechanic.MechanicsData.Count(e => e.Actor == characterName);

				playerModel.Mechanics[mechanic.Name] = playerModel.Mechanics.GetValueOrDefault(mechanic.Name) + count;
			}
		}
	}
}
