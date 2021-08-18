using raidTimelineLogic.Models;
using System.Linq;
using System.Web;

namespace raidTimelineLogic.Mechanics.Strategies
{
	internal class GorsevalMechanics : IMechanics
	{
		internal readonly string EncounterIcon = "https://wiki.guildwars2.com/images/d/d1/Mini_Gorseval_the_Multifarious.png";

		public string CreateHtml(RaidModel model)
		{
			var top = "";
			top += @"<table class=""mechanicsTable"" style=""display: none;"">";
			top += @"<tr style=""color: #aaa"">
						<th>Player</th>
						<th title=""Spectral Impact (KB Slam)"">Slam</th>
						<th title=""Ghastly Prison (Egged)"">Egg</th>
						<th title=""Kicked by small add"">Kick</th>
						<th title=""Hit by Black Goo"">Black</th>
					</tr>";

			foreach (var player in model.Players.OrderByDescending(i => i.Mechanics.Sum(j => j.Value)).Take(3))
			{
				var mid = $@"
					<tr style=""color: #aaa"">
						<td>{HttpUtility.HtmlEncode(player.AccountName)}</td>
						<td>{player.Mechanics["gors_slam"]}</td>
						<td>{player.Mechanics["gors_egg"]}</td>
						<td>{player.Mechanics["gors_kick"]}</td>
						<td>{player.Mechanics["gors_black"]}</td>
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

			var slam = (int)mechanics[0][0];
			var egg = (int)mechanics[1][0];
			var kick = (int)mechanics[2][0];
			var black = (int)mechanics[3][0];

			playerModel.Mechanics.Add("gors_slam", slam);
			playerModel.Mechanics.Add("gors_egg", egg);
			playerModel.Mechanics.Add("gors_kick", kick);
			playerModel.Mechanics.Add("gors_black", black);
		}
	}
}