using raidTimelineLogic.Helper;
using raidTimelineLogic.Models;
using System.Linq;
using System.Web;

namespace raidTimelineLogic.Mechanics.Strategies
{
	internal class SoullessHorrorMechanics : BaseMechanics
	{
		public SoullessHorrorMechanics()
		{
			EncounterIcon = "https://wiki.guildwars2.com/images/d/d4/Mini_Desmina.png";
		}

		public override string CreateHtml(RaidModel model)
		{
			var top = "";
			top += @"<table class=""mechanicsTable"" style=""display: none;"">";
			top += @"<tr style=""color: #aaa"">
						<th>Player</th>
						<th title=""Vortex Slash (Inner Donut hit)"">Donut I.</th>
						<th title=""Vortex Slash (Outer Donut hit)"">Donut O.</th>
						<th title=""Spinning Slash (hit by Scythe)"">Scythe</th>
						<th title=""Quad Slash (4 Slices) & Death Bloom (8 Slices)"">Slices</th>
					</tr>";

			foreach (var player in model.Players.OrderByDescending(i => i.Mechanics.Sum(j => j.Value)).Take(3))
			{
				var mid = $@"
					<tr style=""color: #aaa"">
						<td>{HttpUtility.HtmlEncode(player.AccountName)}</td>
						<td>{player.Mechanics["sh_donutIn"]}</td>
						<td>{player.Mechanics["sh_donutOut"]}</td>
						<td>{player.Mechanics["sh_scythe"]}</td>
						<td>{player.Mechanics["sh_slices"]}</td>
					</tr>";
				top += mid;
			}

			top += "</table>";

			return top;
		}

		public override void Parse(dynamic logData, PlayerModel playerModel)
		{
			PrepareParsing(logData, playerModel);

			var donutIn = playerModel.Mechanics.GetOrDefault("Donut In");
			var donutOut = playerModel.Mechanics.GetOrDefault("Donut Out");
			var scythe = playerModel.Mechanics.GetOrDefault("Scythe");
			var slices = playerModel.Mechanics.GetOrDefault("Slice1") + playerModel.Mechanics.GetOrDefault("Slice2") + playerModel.Mechanics.GetOrDefault("8Slice");

			playerModel.Mechanics.Add("sh_donutIn", donutIn);
			playerModel.Mechanics.Add("sh_donutOut", donutOut);
			playerModel.Mechanics.Add("sh_scythe", scythe);
			playerModel.Mechanics.Add("sh_slices", slices);
		}
	}
}