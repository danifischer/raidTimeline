using raidTimeline.Logic.EiJson;
using raidTimeline.Logic.Helper;
using System.Linq;
using System.Web;
using raidTimeline.Models.Models;

namespace raidTimeline.Logic.Mechanics.Strategies
{
	internal class StatueOfIceMechanics : BaseMechanics
	{
		public StatueOfIceMechanics()
		{
			EncounterIds = new[] { 19691 };
		}

		public override string CreateHtml(RaidModel model)
		{
			var top = "";
			top += @"<table class=""mechanicsTable"" style=""display: none;"">";
			top += @"<tr>
						<th>Player</th>
						<th title=""King's Wrath (Auto Attack Cone Part)"">Cone</th>
						<th title=""Numbing Breach (Ice Cracks in the Ground)"">Cracks</th>
						<th title=""Frozen Wind (Stood in Green)"">Green</th>
					</tr>";

			foreach (var player in model.Players.OrderByDescending(i => i.CombinedMechanics.Where(k => k.Key == "brokenking_cracks").Sum(j => j.Value)).Take(3))
			{
				var mid = $@"
					<tr>
						<td>{HttpUtility.HtmlEncode(player.AccountName)}</td>
						<td>{player.CombinedMechanics["brokenking_cone"]}</td>
						<td>{player.CombinedMechanics["brokenking_cracks"]}</td>
						<td>{player.CombinedMechanics["brokenking_green"]}</td>
					</tr>";
				top += mid;
			}

			top += "</table>";

			return top;
		}

		public override void Parse(EiLog log, PlayerModel playerModel)
		{
			PrepareParsing(log, playerModel);

			var cone = playerModel.Mechanics.GetOrDefault("Cone AoE") + playerModel.Mechanics.GetOrDefault("Cone Shards");
			var cracks = playerModel.Mechanics.GetOrDefault("Cracks");
			var green = playerModel.Mechanics.GetOrDefault("Green");

			playerModel.CombinedMechanics.Add("brokenking_cone", cone);
			playerModel.CombinedMechanics.Add("brokenking_cracks", cracks);
			playerModel.CombinedMechanics.Add("brokenking_green", green);
		}
	}
}