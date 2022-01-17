using raidTimelineLogic.Helper;
using raidTimelineLogic.Mechanics.Strategies;
using raidTimelineLogic.Models;
using System.Linq;
using System.Web;

namespace raidTimelineLogic.Mechanics
{
	internal class XeraMechanics : BaseMechanics
	{
		public XeraMechanics()
		{
			EncounterIcon = "https://wiki.guildwars2.com/images/4/4b/Mini_Xera.png";
		}
		
		public override string CreateHtml(RaidModel model)
		{
			var top = "";
			top += @"<table class=""mechanicsTable"" style=""display: none;"">";
			top += @"<tr style=""color: #aaa"">
						<th>Player</th>
						<th title=""Temporal Shred (Hit by Red Orb)"">Orb</th>
						<th title=""Temporal Shred (Stood in Orb Aoe)"">Orb Aoe</th>
						<th title=""Derangement (Stacking Debuff)"">Stacks</th>
					</tr>";

			foreach (var player in model.Players.OrderByDescending(i => i.CombinedMechanics.Sum(j => j.Value)).Take(3))
			{
				var mid = $@"
					<tr style=""color: #aaa"">
						<td>{HttpUtility.HtmlEncode(player.AccountName)}</td>
						<td>{player.CombinedMechanics["xera_orb"]}</td>
						<td>{player.CombinedMechanics["xera_orbAoe"]}</td>
						<td>{player.CombinedMechanics["xera_stacks"]}</td>
					</tr>";
				top += mid;
			}

			top += "</table>";

			return top;
		}

		public override void Parse(dynamic logData, PlayerModel playerModel)
		{
			PrepareParsing(logData, playerModel);

			var orb = playerModel.Mechanics.GetOrDefault("Orb");
			var orbAoe = playerModel.Mechanics.GetOrDefault("Orb Aoe");
			var stacks = playerModel.Mechanics.GetOrDefault("Stacks");

			playerModel.CombinedMechanics.Add("xera_orb", orb);
			playerModel.CombinedMechanics.Add("xera_orbAoe", orbAoe);
			playerModel.CombinedMechanics.Add("xera_stacks", stacks);
		}
	}
}