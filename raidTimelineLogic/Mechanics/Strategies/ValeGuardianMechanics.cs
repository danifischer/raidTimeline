using raidTimelineLogic.Helper;
using raidTimelineLogic.Mechanics.Strategies;
using raidTimelineLogic.Models;
using System.Linq;
using System.Web;

namespace raidTimelineLogic.Mechanics
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
			top += @"<tr style=""color: #aaa"">
						<th>Player</th>
						<th title=""Unstable Magic Spike (Green Guard or Boss Teleport)"">Ports</th>
						<th title=""Magic Pulse (Hit by Seeker)"">Seeker</th>
						<th title=""Unstable Pylon (Floor dmg)"">Floor</th>
					</tr>";

			foreach (var player in model.Players.OrderByDescending(i => i.Mechanics.Sum(j => j.Value)).Take(3))
			{
				var mid = $@"
					<tr style=""color: #aaa"">
						<td>{HttpUtility.HtmlEncode(player.AccountName)}</td>
						<td>{player.Mechanics["vg_tp"]}</td>
						<td>{player.Mechanics["vg_seeker"]}</td>
						<td>{player.Mechanics["vg_floor"]}</td>
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

			playerModel.Mechanics.Add("vg_tp", tp);
			playerModel.Mechanics.Add("vg_seeker", seeker);
			playerModel.Mechanics.Add("vg_floor", floor);
		}
	}
}