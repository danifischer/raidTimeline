using raidTimeline.Logic.EiJson;
using raidTimeline.Logic.Helper;
using System.Linq;
using System.Web;
using raidTimeline.Models.Models;

namespace raidTimeline.Logic.Mechanics.Strategies
{
	internal class DeimosMechanics : BaseMechanics
	{
		public DeimosMechanics()
		{
			EncounterIds = new[] { 17154 };
		}

		public override string CreateHtml(RaidModel model)
		{
			var top = "";
			top += @"<table class=""mechanicsTable"" style=""display: none;"">";
			top += @"<tr>
						<th>Player</th>
						<th title=""Rapid Decay Trigger (Black expanding oil)"">Oil T.</th>
						<th title=""Annihilate (Cascading Pizza attack)"">Annihilate</th>
						<th title=""Collected a Demonic Tear"">Tear</th>
					</tr>";

			foreach (var player in model.Players.OrderByDescending(i => i.CombinedMechanics.Where(k => k.Key != "dei_tear").Sum(j => j.Value)).Take(3))
			{
				var mid = $@"
					<tr>
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

		public override void Parse(EiLog log, PlayerModel playerModel)
		{
			PrepareParsing(log, playerModel);

			var oil = playerModel.Mechanics.GetOrDefault("Oil T.");
			var pizza = playerModel.Mechanics.GetOrDefault("Pizza");
			var tear = playerModel.Mechanics.GetOrDefault("Tear");

			playerModel.CombinedMechanics.Add("dei_oil", oil);
			playerModel.CombinedMechanics.Add("dei_pizza", pizza);
			playerModel.CombinedMechanics.Add("dei_tear", tear);
		}
	}
}