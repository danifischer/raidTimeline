using raidTimelineLogic.Models;
using System.Collections.Generic;
using raidTimelineLogic.Interfaces;

namespace raidTimelineLogic.Mechanics.Strategies
{
	internal abstract class BaseMechanics : IMechanics
	{
		internal string EncounterIcon;

		public abstract string CreateHtml(RaidModel model);

		public string GetEncounterIcon()
		{
			return EncounterIcon;
		}

		public abstract void Parse(dynamic logData, PlayerModel playerModel);

		internal void PrepareParsing(dynamic logData, PlayerModel playerModel)
		{
			var mechanics = logData.phases[0].mechanicStats[playerModel.Index];
			var header = logData.mechanicMap;
			List<dynamic> mechanicsMap = new List<dynamic>();

			foreach (var mech in header)
			{
				if (mech.playerMech == true)
					mechanicsMap.Add(mech);
			}

			for (int i = 0; i < mechanicsMap.Count; i++)
			{
				playerModel.Mechanics.Add((string)mechanicsMap[i].shortName, (int)mechanics[i][0]);
			}
		}
	}
}