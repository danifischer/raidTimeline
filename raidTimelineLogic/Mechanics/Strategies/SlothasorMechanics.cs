using raidTimelineLogic.Helper;
using raidTimelineLogic.Models;
using System.Linq;
using System.Web;

namespace raidTimelineLogic.Mechanics.Strategies
{
	internal class SlothasorMechanics : BaseMechanics
	{
		public SlothasorMechanics()
		{
			EncounterIcon = "https://wiki.guildwars2.com/images/e/ed/Mini_Slubling.png";
		}

		public override string CreateHtml(RaidModel model)
		{
			var top = "";
			top += @"<table class=""mechanicsTable"" style=""display: none;"">";
			top += @"<tr style=""color: #aaa"">
						<th>Player</th>
						<th title=""Tantrum (Triple Circles after Ground slamming)"">Tantrum</th>
						<th title=""Halitosis (Flame Breath)"">Flame Breath</th>
						<th title=""Stood in Volatile Poison"">Poison dmg</th>
						<th title=""Toxic Cloud (stood in green floor poison)"">Floor Poison</th>
					</tr>";

			foreach (var player in model.Players.OrderByDescending(i => i.CombinedMechanics.Sum(j => j.Value)).Take(3))
			{
				var mid = $@"
					<tr style=""color: #aaa"">
						<td>{HttpUtility.HtmlEncode(player.AccountName)}</td>
						<td>{player.CombinedMechanics["sloth_tantrum"]}</td>
						<td>{player.CombinedMechanics["sloth_breath"]}</td>
						<td>{player.CombinedMechanics["sloth_poisonDmg"]}</td>
						<td>{player.CombinedMechanics["sloth_floor"]}</td>
					</tr>";
				top += mid;
			}

			top += "</table>";

			return top;
		}

		public override void Parse(dynamic logData, PlayerModel playerModel)
		{
			PrepareParsing(logData, playerModel);

			var tantrum = playerModel.Mechanics.GetOrDefault("Tantrum");
			var breath = playerModel.Mechanics.GetOrDefault("Breath");
			var poison = playerModel.Mechanics.GetOrDefault("Poison dmg");
			var floor = playerModel.Mechanics.GetOrDefault("Floor");

			playerModel.CombinedMechanics.Add("sloth_tantrum", tantrum);
			playerModel.CombinedMechanics.Add("sloth_breath", breath);
			playerModel.CombinedMechanics.Add("sloth_poisonDmg", poison);
			playerModel.CombinedMechanics.Add("sloth_floor", floor);
		}
	}
}