using raidTimelineLogic.Helper;
using raidTimelineLogic.Models;
using System.Linq;
using System.Web;

namespace raidTimelineLogic.Mechanics.Strategies
{
	internal class DeimosMechanics : BaseMechanics
	{
		public DeimosMechanics()
		{
			EncounterIcon = "https://wiki.guildwars2.com/images/e/e0/Mini_Ragged_White_Mantle_Figurehead.png";
		}

		public override string CreateHtml(RaidModel model)
		{
			var top = "";
			top += @"<table class=""mechanicsTable"" style=""display: none;"">";
			top += @"<tr style=""color: #aaa"">
						<th>Player</th>
						<th title=""Rapid Decay Trigger (Black expanding oil)"">Oil T.</th>
						<th title=""Annihilate (Cascading Pizza attack)"">Annihilate</th>
						<th title=""Collected a Demonic Tear"">Tear</th>
					</tr>";

			foreach (var player in model.Players.OrderByDescending(i => i.CombinedMechanics.Where(k => k.Key != "dei_tear").Sum(j => j.Value)).Take(3))
			{
				var mid = $@"
					<tr style=""color: #aaa"">
						<td>{HttpUtility.HtmlEncode(player.AccountName)}</td>
						<td>{player.CombinedMechanics["dei_oil"]}</td>
						<td>{player.CombinedMechanics["dei_pizza"]}</td>
						<td>{player.CombinedMechanics["dei_tear"]}</td>
					</tr>";
				top += mid;
			}

			top += "</table>";

			return top;
		}

		public override void Parse(dynamic logData, PlayerModel playerModel)
		{
			PrepareParsing(logData, playerModel);

			var oil = playerModel.Mechanics.GetOrDefault("Oil T.");
			var pizza = playerModel.Mechanics.GetOrDefault("Pizza");
			var tear = playerModel.Mechanics.GetOrDefault("Tear");

			playerModel.CombinedMechanics.Add("dei_oil", oil);
			playerModel.CombinedMechanics.Add("dei_pizza", pizza);
			playerModel.CombinedMechanics.Add("dei_tear", tear);
		}
	}
}