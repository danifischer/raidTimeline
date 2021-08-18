using raidTimelineLogic.Models;
using System.Linq;
using System.Web;

namespace raidTimelineLogic.Mechanics.Strategies
{
	internal class TrioMechanics : IMechanics
	{
		internal readonly string EncounterIcon = "https://i.imgur.com/UZZQUdf.png";

		public string CreateHtml(RaidModel model)
		{
			var top = "";
			top += @"<table class=""mechanicsTable"" style=""display: none;"">";
			top += @"<tr style=""color: #aaa"">
						<th>Player</th>
						<th title=""Hail of Bullets (Zane Cone Shot)"">Zane Cone</th>
						<th title=""Fiery Vortex (Tornado)"">Tornado</th>
					</tr>";

			foreach (var player in model.Players.OrderByDescending(i => i.Mechanics.Sum(j => j.Value)).Take(3))
			{
				var mid = $@"
					<tr style=""color: #aaa"">
						<td>{HttpUtility.HtmlEncode(player.AccountName)}</td>
						<td>{player.Mechanics["trio_cone"]}</td>
						<td>{player.Mechanics["trio_tornado"]}</td>
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

			var cone = (int)mechanics[1][0];
			var tornado = (int)mechanics[2][0];

			playerModel.Mechanics.Add("trio_cone", cone);
			playerModel.Mechanics.Add("trio_tornado", tornado);
		}
	}
}