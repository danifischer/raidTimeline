using raidTimelineLogic.Helper;
using raidTimelineLogic.Models;
using System.Linq;
using System.Web;

namespace raidTimelineLogic.Mechanics.Strategies
{
	internal class CardinalSabirMechanics : BaseMechanics
	{
		public CardinalSabirMechanics()
		{
			EncounterIcon = "https://wiki.guildwars2.com/images/f/fc/Mini_Air_Djinn.png";
		}

		public override string CreateHtml(RaidModel model)
		{
			var top = "";
			top += @"<table class=""mechanicsTable"" style=""display: none;"">";
			top += @"<tr>
						<th>Player</th>
						<th title=""Hit by Shockwave"">Shockwave</th>
					</tr>";

			foreach (var player in model.Players.OrderByDescending(i => i.CombinedMechanics.Sum(j => j.Value)).Take(3))
			{
				var mid = $@"
					<tr>
						<td>{HttpUtility.HtmlEncode(player.AccountName)}</td>
						<td>{player.CombinedMechanics["sabir_shockwave"]}</td>
					</tr>";
				top += mid;
			}

			top += "</table>";

			return top;
		}

		public override void Parse(dynamic logData, PlayerModel playerModel)
		{
			PrepareParsing(logData, playerModel);

			var shockwave = playerModel.Mechanics.GetOrDefault("Shockwave");

			playerModel.CombinedMechanics.Add("sabir_shockwave", shockwave);
		}
	}
}