using System.Linq;
using System.Web;
using raidTimelineLogic.Helper;
using raidTimelineLogic.Models;

namespace raidTimelineLogic.Mechanics.Strategies
{
	internal class ValeGuardianMechanics : BaseMechanics
	{
		public ValeGuardianMechanics()
		{
			EncounterIcon = "https://wiki.guildwars2.com/images/f/fb/Mini_Vale_Guardian.png";
		}

		public override string CreateHtml(RaidModel model)
		{
			var top = "";
			top += @"<table class=""mechanicsTable"" style=""display: none;"">";
			top += @"<tr>
						<th>Player</th>
						<th title=""Unstable Magic Spike (Green Guard or Boss Teleport)"">Ports</th>
						<th title=""Magic Pulse (Hit by Seeker)"">Seeker</th>
						<th title=""Unstable Pylon (Floor dmg)"">Floor</th>
					</tr>";

			foreach (var player in model.Players.OrderByDescending(i => i.CombinedMechanics.Sum(j => j.Value)).Take(3))
			{
				var mid = $@"
					<tr>
						<td>{HttpUtility.HtmlEncode(player.AccountName)}</td>
						<td>{player.CombinedMechanics["vg_tp"]}</td>
						<td>{player.CombinedMechanics["vg_seeker"]}</td>
						<td>{player.CombinedMechanics["vg_floor"]}</td>
					</tr>";
				top += mid;
			}

			top += "</table>";

			return top;
		}

		public override void Parse(dynamic logData, PlayerModel playerModel)
		{
			PrepareParsing(logData, playerModel);

			var tp = playerModel.Mechanics.GetOrDefault("Split TP") + playerModel.Mechanics.GetOrDefault("Boss TP");
			var seeker = playerModel.Mechanics.GetOrDefault("Seeker");
			var floor = playerModel.Mechanics.GetOrDefault("Floor R") + playerModel.Mechanics.GetOrDefault("Floor B") 
				+ playerModel.Mechanics.GetOrDefault("Floor G");

			playerModel.CombinedMechanics.Add("vg_tp", tp);
			playerModel.CombinedMechanics.Add("vg_seeker", seeker);
			playerModel.CombinedMechanics.Add("vg_floor", floor);
		}
	}
}