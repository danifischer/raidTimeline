using raidTimeline.Logic.Helper;
using System.Linq;
using System.Web;
using raidTimeline.Logic.Models;

namespace raidTimeline.Logic.Mechanics.Strategies
{
	internal class MursaatOverseerMechanics : BaseMechanics
	{
		public MursaatOverseerMechanics()
		{
			EncounterIcon = "https://wiki.guildwars2.com/images/c/c8/Mini_Mursaat_Overseer.png";
		}

		public override string CreateHtml(RaidModel model)
		{
			var top = "";
			top += @"<table class=""mechanicsTable"" style=""display: none;"">";
			top += @"<tr>
						<th>Player</th>
						<th title=""Jade Soldier's Aura hit"">Jade</th>
						<th title=""Jade Soldier's Death Explosion"">Expl</th>
					</tr>";

			foreach (var player in model.Players.OrderByDescending(i => i.CombinedMechanics.Sum(j => j.Value)).Take(3))
			{
				var mid = $@"
					<tr>
						<td>{HttpUtility.HtmlEncode(player.AccountName)}</td>
						<td>{player.CombinedMechanics["mo_jade"]}</td>
						<td>{player.CombinedMechanics["mo_explo"]}</td>
					</tr>";
				top += mid;
			}

			top += "</table>";

			return top;
		}

		public override void Parse(dynamic logData, PlayerModel playerModel)
		{
			PrepareParsing(logData, playerModel);

			var jade = playerModel.Mechanics.GetOrDefault("Jade");
			var explo = playerModel.Mechanics.GetOrDefault("Jade Expl");

			playerModel.CombinedMechanics.Add("mo_jade", jade);
			playerModel.CombinedMechanics.Add("mo_explo", explo);
		}
	}
}