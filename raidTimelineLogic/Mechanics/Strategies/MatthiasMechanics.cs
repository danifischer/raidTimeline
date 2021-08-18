using raidTimelineLogic.Models;
using System.Linq;
using System.Web;

namespace raidTimelineLogic.Mechanics.Strategies
{
	internal class MatthiasMechanics : IMechanics
	{
		internal readonly string EncounterIcon = "https://wiki.guildwars2.com/images/5/5d/Mini_Matthias_Abomination.png";

		public string CreateHtml(RaidModel model)
		{
			var top = "";
			top += @"<table class=""mechanicsTable"" style=""display: none;"">";
			top += @"<tr style=""color: #aaa"">
						<th>Player</th>
						<th title=""Shards of Rage (Jump)"">Jump</th>
						<th title=""Tornado & Storm"">Enviroment</th>
						<th title=""Unbalanced (triggered Storm phase Debuff)"">KD</th>
						<th title=""Surrender (hit by walking Spirit)"">Spirit</th>
					</tr>";

			foreach (var player in model.Players.OrderByDescending(i => i.Mechanics.Sum(j => j.Value)).Take(3))
			{
				var mid = $@"
					<tr style=""color: #aaa"">
						<td>{HttpUtility.HtmlEncode(player.AccountName)}</td>
						<td>{player.Mechanics["matt_jump"]}</td>
						<td>{player.Mechanics["matt_enviroment"]}</td>
						<td>{player.Mechanics["matt_kd"]}</td>
						<td>{player.Mechanics["matt_spirit"]}</td>
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

			var jump = (int)mechanics[1][0];
			var enviroment = (int)mechanics[2][0] + (int)mechanics[3][0];
			var spirit = (int)mechanics[13][0];
			var kd = (int)mechanics[10][0];

			playerModel.Mechanics.Add("matt_jump", jump);
			playerModel.Mechanics.Add("matt_enviroment", enviroment);
			playerModel.Mechanics.Add("matt_spirit", spirit);
			playerModel.Mechanics.Add("matt_kd", kd);
		}
	}
}