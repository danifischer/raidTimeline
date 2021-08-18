using raidTimelineLogic.Models;
using System.Linq;
using System.Web;

namespace raidTimelineLogic.Mechanics.Strategies
{
	internal class KeepConstructMechanics : IMechanics
	{
		internal readonly string EncounterIcon = "https://wiki.guildwars2.com/images/e/ea/Mini_Keep_Construct.png";

		public string CreateHtml(RaidModel model)
		{
			var top = "";
			top += @"<table class=""mechanicsTable"" style=""display: none;"">";
			top += @"<tr style=""color: #aaa"">
						<th>Player</th>
						<th title=""Hail of Fury (Falling Debris)"">Debris</th>
						<th title=""Phantasmal Blades (rotating Attack)"">Pizza</th>
						<th title=""Tower Drop (KC Jump)"">Jump</th>
					</tr>";

			foreach (var player in model.Players.OrderByDescending(i => i.Mechanics.Sum(j => j.Value)).Take(3))
			{
				var mid = $@"
					<tr style=""color: #aaa"">
						<td>{HttpUtility.HtmlEncode(player.AccountName)}</td>
						<td>{player.Mechanics["kc_debris"]}</td>
						<td>{player.Mechanics["kc_pizza"]}</td>
						<td>{player.Mechanics["kc_jump"]}</td>
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

			var debris = (int)mechanics[1][0];
			var pizza = (int)mechanics[2][0];
			var jump = (int)mechanics[3][0];

			playerModel.Mechanics.Add("kc_debris", debris);
			playerModel.Mechanics.Add("kc_pizza", pizza);
			playerModel.Mechanics.Add("kc_jump", jump);
		}
	}
}