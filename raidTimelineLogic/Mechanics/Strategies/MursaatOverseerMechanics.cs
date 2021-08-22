using raidTimelineLogic.Helper;
using raidTimelineLogic.Models;
using System.Linq;
using System.Web;

namespace raidTimelineLogic.Mechanics.Strategies
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
			top += @"<tr style=""color: #aaa"">
						<th>Player</th>
						<th title=""Jade Soldier's Aura hit"">Jade</th>
						<th title=""Jade Soldier's Death Explosion"">Expl</th>
					</tr>";

			foreach (var player in model.Players.OrderByDescending(i => i.Mechanics.Sum(j => j.Value)).Take(3))
			{
				var mid = $@"
					<tr style=""color: #aaa"">
						<td>{HttpUtility.HtmlEncode(player.AccountName)}</td>
						<td>{player.Mechanics["mo_jade"]}</td>
						<td>{player.Mechanics["mo_explo"]}</td>
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

			playerModel.Mechanics.Add("mo_jade", jade);
			playerModel.Mechanics.Add("mo_explo", explo);
		}
	}
}