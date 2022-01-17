using raidTimelineLogic.Helper;
using raidTimelineLogic.Models;
using System.Linq;
using System.Web;

namespace raidTimelineLogic.Mechanics.Strategies
{
	internal class MatthiasMechanics : BaseMechanics
	{
		public MatthiasMechanics()
		{
			EncounterIcon = "https://wiki.guildwars2.com/images/5/5d/Mini_Matthias_Abomination.png";
		}

		public override string CreateHtml(RaidModel model)
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

			foreach (var player in model.Players.OrderByDescending(i => i.CombinedMechanics.Sum(j => j.Value)).Take(3))
			{
				var mid = $@"
					<tr style=""color: #aaa"">
						<td>{HttpUtility.HtmlEncode(player.AccountName)}</td>
						<td>{player.CombinedMechanics["matt_jump"]}</td>
						<td>{player.CombinedMechanics["matt_enviroment"]}</td>
						<td>{player.CombinedMechanics["matt_kd"]}</td>
						<td>{player.CombinedMechanics["matt_spirit"]}</td>
					</tr>";
				top += mid;
			}

			top += "</table>";

			return top;
		}

		public override void Parse(dynamic logData, PlayerModel playerModel)
		{
			PrepareParsing(logData, playerModel);

			var jump = playerModel.Mechanics.GetOrDefault("Jump Shards");
			var enviroment = playerModel.Mechanics.GetOrDefault("Tornado") + playerModel.Mechanics.GetOrDefault("Storm");
			var spirit = playerModel.Mechanics.GetOrDefault("Spirit");
			var kd = playerModel.Mechanics.GetOrDefault("KD");

			playerModel.CombinedMechanics.Add("matt_jump", jump);
			playerModel.CombinedMechanics.Add("matt_enviroment", enviroment);
			playerModel.CombinedMechanics.Add("matt_spirit", spirit);
			playerModel.CombinedMechanics.Add("matt_kd", kd);
		}
	}
}