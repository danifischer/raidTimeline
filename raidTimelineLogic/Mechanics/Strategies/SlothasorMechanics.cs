using raidTimelineLogic.Models;
using System.Linq;
using System.Web;

namespace raidTimelineLogic.Mechanics.Strategies
{
	internal class SlothasorMechanics : IMechanics
	{
		internal readonly string EncounterIcon = "https://wiki.guildwars2.com/images/e/ed/Mini_Slubling.png";

		public string CreateHtml(RaidModel model)
		{
			var top = "";
			top += @"<table class=""mechanicsTable"" style=""display: none;"">";
			top += @"<tr style=""color: #aaa"">
						<th>Player</th>
						<th title=""Tantrum (Triple Circles after Ground slamming)"">Tantrum</th>
						<th title=""Halitosis (Flame Breath)"">Flame Breath</th>
						<th title=""Toxic Cloud (stood in green floor poison)"">Floor Poison</th>
					</tr>";

			foreach (var player in model.Players.OrderByDescending(i => i.Mechanics.Sum(j => j.Value)).Take(3))
			{
				var mid = $@"
					<tr style=""color: #aaa"">
						<td>{HttpUtility.HtmlEncode(player.AccountName)}</td>
						<td>{player.Mechanics["sloth_tantrum"]}</td>
						<td>{player.Mechanics["sloth_breath"]}</td>
						<td>{player.Mechanics["sloth_floor"]}</td>
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

			var tantrum = (int)mechanics[0][0];
			var breath = (int)mechanics[2][0];
			var floor = (int)mechanics[6][0];

			playerModel.Mechanics.Add("sloth_tantrum", tantrum);
			playerModel.Mechanics.Add("sloth_breath", breath);
			playerModel.Mechanics.Add("sloth_floor", floor);
		}
	}
}