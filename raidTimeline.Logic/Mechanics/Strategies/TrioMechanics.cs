using raidTimeline.Logic.Helper;
using System.Linq;
using System.Web;
using raidTimeline.Logic.Models;

namespace raidTimeline.Logic.Mechanics.Strategies
{
	internal class TrioMechanics : BaseMechanics
	{
		public TrioMechanics()
		{
			EncounterIcon = "https://i.imgur.com/UZZQUdf.png";
		}
		
		public override string CreateHtml(RaidModel model)
		{
			var top = "";
			top += @"<table class=""mechanicsTable"" style=""display: none;"">";
			top += @"<tr>
						<th>Player</th>
						<th title=""Hail of Bullets (Zane Cone Shot)"">Zane Cone</th>
						<th title=""Fiery Vortex (Tornado)"">Tornado</th>
					</tr>";

			foreach (var player in model.Players.OrderByDescending(i => i.CombinedMechanics.Sum(j => j.Value)).Take(3))
			{
				var mid = $@"
					<tr>
						<td>{HttpUtility.HtmlEncode(player.AccountName)}</td>
						<td>{player.CombinedMechanics["trio_cone"]}</td>
						<td>{player.CombinedMechanics["trio_tornado"]}</td>
					</tr>";
				top += mid;
			}

			top += "</table>";

			return top;
		}

		public override void Parse(dynamic logData, PlayerModel playerModel)
		{
			PrepareParsing(logData, playerModel);

			var cone = playerModel.Mechanics.GetOrDefault("Zane Cone");
			var tornado = playerModel.Mechanics.GetOrDefault("Tornado");

			playerModel.CombinedMechanics.Add("trio_cone", cone);
			playerModel.CombinedMechanics.Add("trio_tornado", tornado);
		}
	}
}