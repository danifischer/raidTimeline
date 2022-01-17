using raidTimelineLogic.Helper;
using raidTimelineLogic.Models;
using System.Linq;
using System.Web;

namespace raidTimelineLogic.Mechanics.Strategies
{
	internal class ConjuredAmalgamateMechanics : BaseMechanics
	{
		public ConjuredAmalgamateMechanics()
		{
			EncounterIcon = "https://i.imgur.com/eLyIWd2.png";
		}

		public override string CreateHtml(RaidModel model)
		{
			var top = "";
			top += @"<table class=""mechanicsTable"" style=""display: none;"">";
			top += @"<tr style=""color: #aaa"">
						<th>Player</th>
						<th title=""Pulverize (Arm Slam)"">Slam</th>
						<th title=""Junk Absorption (Purple Balls during collect)"">Balls</th>
						<th title=""Junk Fall (Falling Debris)"">Junk</th>
						<th title=""Tremor (Field adjacent to Arm Slam)"">Tremor</th>
					</tr>";

			foreach (var player in model.Players.OrderByDescending(i => i.CombinedMechanics.Sum(j => j.Value)).Take(3))
			{
				var mid = $@"
					<tr style=""color: #aaa"">
						<td>{HttpUtility.HtmlEncode(player.AccountName)}</td>
						<td>{player.CombinedMechanics["ca_arm"]}</td>
						<td>{player.CombinedMechanics["ca_balls"]}</td>
						<td>{player.CombinedMechanics["ca_junk"]}</td>
						<td>{player.CombinedMechanics["ca_tremor"]}</td>
					</tr>";
				top += mid;
			}

			top += "</table>";

			return top;
		}

		public override void Parse(dynamic logData, PlayerModel playerModel)
		{
			PrepareParsing(logData, playerModel);

			var arm = playerModel.Mechanics.GetOrDefault("Arm Slam") - playerModel.Mechanics.GetOrDefault("Stab.Slam");
			var balls = playerModel.Mechanics.GetOrDefault("Balls");
			var junk = playerModel.Mechanics.GetOrDefault("Junk");
			var tremor = playerModel.Mechanics.GetOrDefault("Tremor");

			playerModel.CombinedMechanics.Add("ca_arm", arm);
			playerModel.CombinedMechanics.Add("ca_balls", balls);
			playerModel.CombinedMechanics.Add("ca_junk", junk);
			playerModel.CombinedMechanics.Add("ca_tremor", tremor);
		}
	}
}