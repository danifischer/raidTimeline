using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using raidTimeline.Logic.Interfaces;
using raidTimeline.Models.Models;

namespace raidTimeline.Logic.Mechanics.Strategies
{
	internal abstract class BaseMechanics : IMechanics
	{
		internal string EncounterIcon;

		public abstract string CreateHtml(RaidModel model);

		public string GetEncounterIcon()
		{
			return EncounterIcon;
		}

		public abstract void Parse(JObject logData, PlayerModel playerModel);

		internal static void PrepareParsing(JObject logData, PlayerModel playerModel)
		{
			var mechanics = logData["phases"][0]["mechanicStats"][playerModel.Index];
			var header = logData["mechanicMap"];
			var mechanicsMap = new List<JToken>();

			foreach (var mech in header)
			{
				if (mech["playerMech"].Value<bool>())
					mechanicsMap.Add(mech);
			}

			for (int i = 0; i < mechanicsMap.Count; i++)
			{
				playerModel.Mechanics.Add(mechanicsMap[i]["shortName"].Value<string>(), mechanics[i][0].Value<int>());
			}
		}
	}
}