using raidTimeline.Logic.Helper;
using System.Linq;
using System.Web;
using raidTimeline.Logic.Models;

namespace raidTimeline.Logic.Mechanics.Strategies
{
	internal class TwinLargosMechanics : BaseMechanics
	{
		public TwinLargosMechanics()
		{
			EncounterIcon = "https://i.imgur.com/6O5MT7v.png";
		}
		
		public override string CreateHtml(RaidModel model)
		{
			var top = "";
			top += @"<table class=""mechanicsTable"" style=""display: none;"">";
			top += @"<tr>
						<th>Player</th>
						<th title=""Waterlogged (stacking water debuff)"">Debuff</th>
						<th title=""Kenut mechanics (Wave, Tornado & Y Field)"">Kenut</th>
						<th title=""Nikare mechanics (Charge, Pool, Bubbles & Lauch Fields)"">Nikare</th>
					</tr>";

			foreach (var player in model.Players.OrderByDescending(i => i.CombinedMechanics.Sum(j => j.Value)).Take(3))
			{
				var mid = $@"
					<tr>
						<td>{HttpUtility.HtmlEncode(player.AccountName)}</td>
						<td>{player.CombinedMechanics["twinlargos_debuff"]}</td>
						<td>{player.CombinedMechanics["twinlargos_kenut"]}</td>
						<td>{player.CombinedMechanics["twinlargos_nikare"]}</td>
					</tr>";
				top += mid;
			}

			top += "</table>";

			return top;
		}

		public override void Parse(dynamic logData, PlayerModel playerModel)
		{
			PrepareParsing(logData, playerModel);

			var debuff = playerModel.Mechanics.GetOrDefault("Debuff");
			var kenut = playerModel.Mechanics.GetOrDefault("Wave") + playerModel.Mechanics.GetOrDefault("Tornado") 
				+ playerModel.Mechanics.GetOrDefault("Y Field");
			var nikare = playerModel.Mechanics.GetOrDefault("Charge") + playerModel.Mechanics.GetOrDefault("Pool") 
				+ playerModel.Mechanics.GetOrDefault("KB/Launch") + playerModel.Mechanics.GetOrDefault("Float");
			
			playerModel.CombinedMechanics.Add("twinlargos_debuff", debuff);
			playerModel.CombinedMechanics.Add("twinlargos_kenut", kenut);
			playerModel.CombinedMechanics.Add("twinlargos_nikare", nikare);
		}
	}
}

/*
 * 0	Debuff		Water debuf					All
 * 1	Charge		Charge						Nik
 * 2	Pool		Pool damage					Nik
 * 3	Wave		Sea Swell					Ken
 * 4	Geyser		Launching aoe fields		Nik
 * 5	Pool mech	Got pool mech				-
 * 6	Float		Bubbles						Nik
 * 7	Tornado		Tornado dmg					Ken
 * 8	Ken Aura
 * 9	Nik Aura
 * 10	Y Field		Lasers Ken					Ken
 */