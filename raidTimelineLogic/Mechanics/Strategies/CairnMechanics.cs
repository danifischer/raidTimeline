using raidTimelineLogic.Models;
using System.Linq;
using System.Web;

namespace raidTimelineLogic.Mechanics.Strategies
{
	internal class CairnMechanics : IMechanics
	{
		internal readonly string EncounterIcon = "https://wiki.guildwars2.com/images/b/b8/Mini_Cairn_the_Indomitable.png";

		public string CreateHtml(RaidModel model)
		{
			var top = "";
			top += @"<table class=""mechanicsTable"" style=""display: none;"">";
			top += @"<tr style=""color: #aaa"">
						<th>Player</th>
						<th title=""Orange Teleport Field"">Port</th>
						<th title=""Green Spatial Manipulation Field (lift)"">Green</th>
						<th title=""Knockback Crystals (tornado like)"">KB</th>
						<th title=""Leap, Sweep & Donut"">Boss attacks</th>
					</tr>";

			foreach (var player in model.Players.OrderByDescending(i => i.Mechanics.Sum(j => j.Value)).Take(3))
			{
				var mid = $@"
					<tr style=""color: #aaa"">
						<td>{HttpUtility.HtmlEncode(player.AccountName)}</td>
						<td>{player.Mechanics["cairn_port"]}</td>
						<td>{player.Mechanics["cairn_green"]}</td>
						<td>{player.Mechanics["cairn_kb"]}</td>
						<td>{player.Mechanics["cairn_blackStuff"]}</td>
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

			var port = (int)mechanics[0][0];
			var green = (int)mechanics[1][0] - (int)mechanics[2][0];
			var kb = (int)mechanics[4][0];
			var blackStuff = (int)mechanics[8][0] + (int)mechanics[9][0] + (int)mechanics[10][0];

			playerModel.Mechanics.Add("cairn_port", port);
			playerModel.Mechanics.Add("cairn_green", green);
			playerModel.Mechanics.Add("cairn_kb", kb);
			playerModel.Mechanics.Add("cairn_blackStuff", blackStuff);
		}
	}
}