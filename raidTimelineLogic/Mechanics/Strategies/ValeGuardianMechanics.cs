using raidTimelineLogic.Models;
using System.Linq;
using System.Web;

namespace raidTimelineLogic.Mechanics
{
	internal class ValeGuardianMechanics : IMechanics
	{
		internal readonly string EncounterIcon = "https://wiki.guildwars2.com/images/f/fb/Mini_Vale_Guardian.png";

		public string CreateHtml(RaidModel model)
		{
			var top = "";
			top += @"<table class=""mechanicsTable"" style=""display: none;"">";
			top += @"<tr style=""color: #aaa"">
						<th>Player</th>
						<th title=""Unstable Magic Spike (Green Guard or Boss Teleport)"">Ports</th>
						<th title=""Magic Pulse (Hit by Seeker)"">Seeker</th>
						<th title=""Unstable Pylon (Floor dmg)"">Floor</th>
					</tr>";

			foreach (var player in model.Players.OrderByDescending(i => i.Mechanics.Sum(j => j.Value)).Take(3))
			{
				var mid = $@"
					<tr style=""color: #aaa"">
						<td>{HttpUtility.HtmlEncode(player.AccountName)}</td>
						<td>{player.Mechanics["vg_tp"]}</td>
						<td>{player.Mechanics["vg_seeker"]}</td>
						<td>{player.Mechanics["vg_floor"]}</td>
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

			var tp = (int)mechanics[0][0] + (int)mechanics[1][0];
			var seeker = (int)mechanics[3][0];
			var floor = (int)mechanics[6][0] + (int)mechanics[7][0] + (int)mechanics[8][0];

			playerModel.Mechanics.Add("vg_tp", tp);
			playerModel.Mechanics.Add("vg_seeker", seeker);
			playerModel.Mechanics.Add("vg_floor", floor);
		}
	}
}