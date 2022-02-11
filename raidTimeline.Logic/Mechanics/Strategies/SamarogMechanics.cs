using raidTimeline.Logic.Helper;
using System.Linq;
using System.Web;
using raidTimeline.Logic.Models;

namespace raidTimeline.Logic.Mechanics.Strategies
{
	internal class SamarogMechanics : BaseMechanics
	{
		public SamarogMechanics()
		{
			EncounterIcon = "https://wiki.guildwars2.com/images/f/f0/Mini_Samarog.png";
		}
		
		public override string CreateHtml(RaidModel model)
		{
			var top = "";
			top += @"<table class=""mechanicsTable"" style=""display: none;"">";
			top += @"<tr>
						<th>Player</th>
						<th title=""Samarog Attacks (Wave, Slam, Rush & Sweep)"">Attacks</th>
						<th title=""Inevitable Betrayal (failed Green)"">F. Fail</th>
						<th title=""Spear spawn and attacks"">Spear</th>
					</tr>";

			foreach (var player in model.Players.OrderByDescending(i => i.CombinedMechanics.Sum(j => j.Value)).Take(3))
			{
				var mid = $@"
					<tr>
						<td>{HttpUtility.HtmlEncode(player.AccountName)}</td>
						<td>{player.CombinedMechanics["sam_attacks"]}</td>
						<td>{player.CombinedMechanics["sam_friendFail"]}</td>
						<td>{player.CombinedMechanics["sam_spear"]}</td>
					</tr>";
				top += mid;
			}

			top += "</table>";

			return top;
		}

		public override void Parse(dynamic logData, PlayerModel playerModel)
		{
			PrepareParsing(logData, playerModel);

			var samAttacks = playerModel.Mechanics.GetOrDefault("Schk.Wv") + playerModel.Mechanics.GetOrDefault("Swp") + playerModel.Mechanics.GetOrDefault("Trpl") + playerModel.Mechanics.GetOrDefault("Slam");
			var friendFail = playerModel.Mechanics.GetOrDefault("Gr.Fl");
			var spear = playerModel.Mechanics.GetOrDefault("S.Pls") + playerModel.Mechanics.GetOrDefault("S.Spwn") + playerModel.Mechanics.GetOrDefault("Shck.Wv Ctr");

			playerModel.CombinedMechanics.Add("sam_attacks", samAttacks);
			playerModel.CombinedMechanics.Add("sam_friendFail", friendFail);
			playerModel.CombinedMechanics.Add("sam_spear", spear);
		}
	}
}