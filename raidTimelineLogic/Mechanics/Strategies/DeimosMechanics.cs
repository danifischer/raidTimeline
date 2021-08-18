using raidTimelineLogic.Models;
using System.Linq;
using System.Web;

namespace raidTimelineLogic.Mechanics.Strategies
{
	internal class DeimosMechanics : IMechanics
	{
		internal readonly string EncounterIcon = "https://wiki.guildwars2.com/images/e/e0/Mini_Ragged_White_Mantle_Figurehead.png";

		public string CreateHtml(RaidModel model)
		{
			var top = "";
			top += @"<table class=""mechanicsTable"" style=""display: none;"">";
			top += @"<tr style=""color: #aaa"">
						<th>Player</th>
						<th title=""Rapid Decay Trigger (Black expanding oil)"">Oil T.</th>
						<th title=""Annihilate (Cascading Pizza attack)"">Annihilate</th>
						<th title=""Collected a Demonic Tear"">Tear</th>
					</tr>";

			foreach (var player in model.Players.OrderByDescending(i => i.Mechanics.Where(k => k.Key != "dei_tear").Sum(j => j.Value)).Take(3))
			{
				var mid = $@"
					<tr style=""color: #aaa"">
						<td>{HttpUtility.HtmlEncode(player.AccountName)}</td>
						<td>{player.Mechanics["dei_oil"]}</td>
						<td>{player.Mechanics["dei_pizza"]}</td>
						<td>{player.Mechanics["dei_tear"]}</td>
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

			var oil = (int)mechanics[1][0];
			var pizza = (int)mechanics[2][0];
			var tear = (int)mechanics[4][0];

			playerModel.Mechanics.Add("dei_oil", oil);
			playerModel.Mechanics.Add("dei_pizza", pizza);
			playerModel.Mechanics.Add("dei_tear", tear);
		}
	}
}