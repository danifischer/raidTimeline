using raidTimelineLogic.Models;
using System.Linq;
using System.Web;

namespace raidTimelineLogic.Mechanics.Strategies
{
	internal class SabethaMechanics : IMechanics
	{
		internal readonly string EncounterIcon = "https://wiki.guildwars2.com/images/5/54/Mini_Sabetha.png";

		public string CreateHtml(RaidModel model)
		{
			var top = "";
			top += @"<table class=""mechanicsTable"" style=""display: none;"">";
			top += @"<tr style=""color: #aaa"">
						<th>Player</th>
						<th title=""Flak Shot (Fire Patches)"">Flak</th>
						<th title=""Cannon Barrage (stood in AoE)"">Cannon</th>
						<th title=""Flame Blast (Karde's Flamethrower)"">Flamethrower</th>
					</tr>";

			foreach (var player in model.Players.OrderByDescending(i => i.Mechanics.Sum(j => j.Value)).Take(3))
			{
				var mid = $@"
					<tr style=""color: #aaa"">
						<td>{HttpUtility.HtmlEncode(player.AccountName)}</td>
						<td>{player.Mechanics["sab_flak"]}</td>
						<td>{player.Mechanics["sab_cannon"]}</td>
						<td>{player.Mechanics["sab_flamethrower"]}</td>
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

			var flak = (int)mechanics[3][0];
			var cannon = (int)mechanics[4][0];
			var flamethrower = (int)mechanics[5][0];

			playerModel.Mechanics.Add("sab_flak", flak);
			playerModel.Mechanics.Add("sab_cannon", cannon);
			playerModel.Mechanics.Add("sab_flamethrower", flamethrower);
		}
	}
}