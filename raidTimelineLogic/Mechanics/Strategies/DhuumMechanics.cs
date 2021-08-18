using raidTimelineLogic.Models;
using System.Linq;
using System.Web;

namespace raidTimelineLogic.Mechanics.Strategies
{
	internal class DhuumMechanics : IMechanics
	{
		internal readonly string EncounterIcon = "https://wiki.guildwars2.com/images/e/e4/Mini_Dhuum.png";

		public string CreateHtml(RaidModel model)
		{
			var top = "";
			top += @"<table class=""mechanicsTable"" style=""display: none;"">";
			top += @"<tr style=""color: #aaa"">
						<th>Player</th>
						<th title=""Boon ripping Cone Attack"">Cone</th>
						<th title=""Cull (Fearing Fissures)"">Crack</th>
						<th title=""Necro Marks during Scythe attack"">Mark</th>
						<th title=""Lesser Death Mark hit (Dip into ground)"">Dip</th>
					</tr>";

			foreach (var player in model.Players.OrderByDescending(i => i.Mechanics.Sum(j => j.Value)).Take(3))
			{
				var mid = $@"
					<tr style=""color: #aaa"">
						<td>{HttpUtility.HtmlEncode(player.AccountName)}</td>
						<td>{player.Mechanics["dhuum_cone"]}</td>
						<td>{player.Mechanics["dhuum_crack"]}</td>
						<td>{player.Mechanics["dhuum_mark"]}</td>
						<td>{player.Mechanics["dhuum_dip"]}</td>
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

			var cone = (int)mechanics[5][0];
			var crack = (int)mechanics[6][0];
			var mark = (int)mechanics[7][0];
			var dip = (int)mechanics[8][0];

			playerModel.Mechanics.Add("dhuum_cone", cone);
			playerModel.Mechanics.Add("dhuum_crack", crack);
			playerModel.Mechanics.Add("dhuum_mark", mark);
			playerModel.Mechanics.Add("dhuum_dip", dip);
		}
	}
}