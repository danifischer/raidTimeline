using raidTimeline.Logic.Helper;
using System.Linq;
using System.Web;
using raidTimeline.Logic.Models;

namespace raidTimeline.Logic.Mechanics.Strategies
{
	internal class DhuumMechanics : BaseMechanics
	{
		public DhuumMechanics()
		{
			EncounterIcon = "https://wiki.guildwars2.com/images/e/e4/Mini_Dhuum.png";
		}

		public override string CreateHtml(RaidModel model)
		{
			var top = "";
			top += @"<table class=""mechanicsTable"" style=""display: none;"">";
			top += @"<tr>
						<th>Player</th>
						<th title=""Boon ripping Cone Attack"">Cone</th>
						<th title=""Cull (Fearing Fissures)"">Crack</th>
						<th title=""Necro Marks during Scythe attack"">Mark</th>
						<th title=""Lesser Death Mark hit (Dip into ground)"">Dip</th>
						<th title=""Picked up by Ender's Echo"">Echo</th>
					</tr>";

			foreach (var player in model.Players.OrderByDescending(i => i.CombinedMechanics.Sum(j => j.Value)).Take(3))
			{
				var mid = $@"
					<tr>
						<td>{HttpUtility.HtmlEncode(player.AccountName)}</td>
						<td>{player.CombinedMechanics["dhuum_cone"]}</td>
						<td>{player.CombinedMechanics["dhuum_crack"]}</td>
						<td>{player.CombinedMechanics["dhuum_mark"]}</td>
						<td>{player.CombinedMechanics["dhuum_dip"]}</td>
						<td>{player.CombinedMechanics["dhuum_echo"]}</td>
					</tr>";
				top += mid;
			}

			top += "</table>";

			return top;
		}

		public override void Parse(dynamic logData, PlayerModel playerModel)
		{
			PrepareParsing(logData, playerModel);

			var cone = playerModel.Mechanics.GetOrDefault("Cone");
			var crack = playerModel.Mechanics.GetOrDefault("Crack");
			var mark = playerModel.Mechanics.GetOrDefault("Mark");
			var dip = playerModel.Mechanics.GetOrDefault("Dip");
			var echo = playerModel.Mechanics.GetOrDefault("Echo PU");

			playerModel.CombinedMechanics.Add("dhuum_cone", cone);
			playerModel.CombinedMechanics.Add("dhuum_crack", crack);
			playerModel.CombinedMechanics.Add("dhuum_mark", mark);
			playerModel.CombinedMechanics.Add("dhuum_dip", dip);
			playerModel.CombinedMechanics.Add("dhuum_echo", echo);
		}
	}
}