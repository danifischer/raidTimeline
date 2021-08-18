using raidTimelineLogic.Models;
using System.Linq;
using System.Web;

namespace raidTimelineLogic.Mechanics.Strategies
{
	internal class StatueOfIceMechanics : IMechanics
	{
		internal readonly string EncounterIcon = "https://wiki.guildwars2.com/images/3/37/Mini_Broken_King.png";

		public string CreateHtml(RaidModel model)
		{
			var top = "";
			top += @"<table class=""mechanicsTable"" style=""display: none;"">";
			top += @"<tr style=""color: #aaa"">
						<th>Player</th>
						<th title=""King's Wrath (Auto Attack Cone Part)"">Cone</th>
						<th title=""Numbing Breach (Ice Cracks in the Ground)"">Cracks</th>
						<th title=""Frozen Wind (Stood in Green)"">Green</th>
					</tr>";

			foreach (var player in model.Players.OrderByDescending(i => i.Mechanics.Where(k => k.Key == "brokenking_cracks").Sum(j => j.Value)).Take(3))
			{
				var mid = $@"
					<tr style=""color: #aaa"">
						<td>{HttpUtility.HtmlEncode(player.AccountName)}</td>
						<td>{player.Mechanics["brokenking_cone"]}</td>
						<td>{player.Mechanics["brokenking_cracks"]}</td>
						<td>{player.Mechanics["brokenking_green"]}</td>
					</tr>";
				top += mid;
			}

			top += "</table>";

			return top;
		}

		public string GetEncounterIcon()
		{
			return EncounterIcon;
		}

		public void Parse(dynamic logData, PlayerModel playerModel)
		{
			var mechanics = logData.phases[0].mechanicStats[playerModel.Index];

			var cone = (int)mechanics[0][0];
			var cracks = (int)mechanics[1][0];
			var green = (int)mechanics[2][0];

			playerModel.Mechanics.Add("brokenking_cone", cone);
			playerModel.Mechanics.Add("brokenking_cracks", cracks);
			playerModel.Mechanics.Add("brokenking_green", green);
		}
	}
}