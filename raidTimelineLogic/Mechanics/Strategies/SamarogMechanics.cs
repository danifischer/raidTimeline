using raidTimelineLogic.Models;
using System.Linq;
using System.Web;

namespace raidTimelineLogic.Mechanics.Strategies
{
	internal class SamarogMechanics : IMechanics
	{
		internal readonly string EncounterIcon = "https://wiki.guildwars2.com/images/f/f0/Mini_Samarog.png";

		public string CreateHtml(RaidModel model)
		{
			var top = "";
			top += @"<table class=""mechanicsTable"" style=""display: none;"">";
			top += @"<tr style=""color: #aaa"">
						<th>Player</th>
						<th title=""Samarog Attacks (Wave, Slam & Sweep)"">Attacks</th>
						<th title=""Inevitable Betrayal (failed Green)"">F. Fail</th>
						<th title=""Spear spawn and attacks"">Spear</th>
						<th title=""Effigy Pulse (Stood in Spear AoE)"">R. Expl</th>
					</tr>";

			foreach (var player in model.Players.OrderByDescending(i => i.Mechanics.Sum(j => j.Value)).Take(3))
			{
				var mid = $@"
					<tr style=""color: #aaa"">
						<td>{HttpUtility.HtmlEncode(player.AccountName)}</td>
						<td>{player.Mechanics["sam_attacks"]}</td>
						<td>{player.Mechanics["sam_friendFail"]}</td>
						<td>{player.Mechanics["sam_spear"]}</td>
						<td>{player.Mechanics["sam_rigom"]}</td>
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

			var samAttacks = (int)mechanics[0][0] + (int)mechanics[1][0] + (int)mechanics[2][0];
			var friendFail = (int)mechanics[8][0];
			var spear = (int)mechanics[13][0] + (int)mechanics[10][0];
			var rigom = (int)mechanics[9][0];

			playerModel.Mechanics.Add("sam_attacks", samAttacks);
			playerModel.Mechanics.Add("sam_friendFail", friendFail);
			playerModel.Mechanics.Add("sam_spear", spear);
			playerModel.Mechanics.Add("sam_rigom", rigom);
		}
	}
}