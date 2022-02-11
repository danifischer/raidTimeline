using raidTimeline.Logic.Helper;
using System.Linq;
using System.Web;
using raidTimeline.Logic.Models;

namespace raidTimeline.Logic.Mechanics.Strategies
{
	internal class CairnMechanics : BaseMechanics
	{
		public CairnMechanics()
		{
			EncounterIcon = "https://wiki.guildwars2.com/images/b/b8/Mini_Cairn_the_Indomitable.png";
		}

		public override string CreateHtml(RaidModel model)
		{
			var top = "";
			top += @"<table class=""mechanicsTable"" style=""display: none;"">";
			top += @"<tr>
						<th>Player</th>
						<th title=""Orange Teleport Field"">Port</th>
						<th title=""Green Spatial Manipulation Field (lift)"">Green</th>
						<th title=""Knockback Crystals (tornado like)"">KB</th>
						<th title=""Leap, Sweep & Donut"">Boss attacks</th>
					</tr>";

			foreach (var player in model.Players.OrderByDescending(i => i.CombinedMechanics.Sum(j => j.Value)).Take(3))
			{
				var mid = $@"
					<tr>
						<td>{HttpUtility.HtmlEncode(player.AccountName)}</td>
						<td>{player.CombinedMechanics["cairn_port"]}</td>
						<td>{player.CombinedMechanics["cairn_green"]}</td>
						<td>{player.CombinedMechanics["cairn_kb"]}</td>
						<td>{player.CombinedMechanics["cairn_blackStuff"]}</td>
					</tr>";
				top += mid;
			}

			top += "</table>";

			return top;
		}

		public override void Parse(dynamic logData, PlayerModel playerModel)
		{
			PrepareParsing(logData, playerModel);

			var port = playerModel.Mechanics.GetOrDefault("Port");
			var green = playerModel.Mechanics.GetOrDefault("Green") - playerModel.Mechanics.GetOrDefault("Stab.Green");
			var kb = playerModel.Mechanics.GetOrDefault("KB");
			var blackStuff = playerModel.Mechanics.GetOrDefault("Leap") + playerModel.Mechanics.GetOrDefault("Sweep") + playerModel.Mechanics.GetOrDefault("Donut");

			playerModel.CombinedMechanics.Add("cairn_port", port);
			playerModel.CombinedMechanics.Add("cairn_green", green);
			playerModel.CombinedMechanics.Add("cairn_kb", kb);
			playerModel.CombinedMechanics.Add("cairn_blackStuff", blackStuff);
		}
	}
}