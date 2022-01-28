using raidTimelineLogic.Helper;
using raidTimelineLogic.Models;
using System.Linq;
using System.Web;

namespace raidTimelineLogic.Mechanics.Strategies
{
	internal class GorsevalMechanics : BaseMechanics
	{
		public GorsevalMechanics()
		{
			EncounterIcon = "https://wiki.guildwars2.com/images/d/d1/Mini_Gorseval_the_Multifarious.png";
		}

		public override string CreateHtml(RaidModel model)
		{
			var top = "";
			top += @"<table class=""mechanicsTable"" style=""display: none;"">";
			top += @"<tr>
						<th>Player</th>
						<th title=""Spectral Impact (KB Slam)"">Slam</th>
						<th title=""Ghastly Prison (Egged)"">Egg</th>
						<th title=""Kicked by small add"">Kick</th>
						<th title=""Hit by Black Goo"">Black</th>
					</tr>";

			foreach (var player in model.Players.OrderByDescending(i => i.CombinedMechanics.Sum(j => j.Value)).Take(3))
			{
				var mid = $@"
					<tr>
						<td>{HttpUtility.HtmlEncode(player.AccountName)}</td>
						<td>{player.CombinedMechanics["gors_slam"]}</td>
						<td>{player.CombinedMechanics["gors_egg"]}</td>
						<td>{player.CombinedMechanics["gors_kick"]}</td>
						<td>{player.CombinedMechanics["gors_black"]}</td>
					</tr>";
				top += mid;
			}

			top += "</table>";

			return top;
		}

		public override void Parse(dynamic logData, PlayerModel playerModel)
		{
			PrepareParsing(logData, playerModel);

			var slam = playerModel.Mechanics.GetOrDefault("Slam");
			var egg = playerModel.Mechanics.GetOrDefault("Egg");
			var kick = playerModel.Mechanics.GetOrDefault("Kick");
			var black = playerModel.Mechanics.GetOrDefault("Black");

			playerModel.CombinedMechanics.Add("gors_slam", slam);
			playerModel.CombinedMechanics.Add("gors_egg", egg);
			playerModel.CombinedMechanics.Add("gors_kick", kick);
			playerModel.CombinedMechanics.Add("gors_black", black);
		}
	}
}