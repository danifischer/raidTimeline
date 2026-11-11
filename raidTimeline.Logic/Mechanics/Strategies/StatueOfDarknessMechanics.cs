using raidTimeline.Logic.EiJson;
using raidTimeline.Logic.Helper;
using System.Linq;
using System.Web;
using raidTimeline.Models.Models;

namespace raidTimeline.Logic.Mechanics.Strategies
{
	internal class StatueOfDarknessMechanics : BaseMechanics
	{
		public StatueOfDarknessMechanics()
		{
			EncounterIds = new[] { 19844, 19651 };
		}

		public override string CreateHtml(RaidModel model)
		{
			var top = "";
			top += @"<table class=""mechanicsTable"" style=""display: none;"">";
			top += @"<tr>
						<th>Player</th>
						<th title=""Light Carrier (picked up a light orb)"">Light Orb</th>
						<th title=""Flare (detonate light orb to incapacitate eye)"">Detonate</th>
						<th title=""Deep Abyss (ticking eye beam)"">Beam</th>
					</tr>";

			foreach (var player in model.Players.OrderByDescending(i => i.CombinedMechanics.Where(k => k.Key == "eyes_beam").Sum(j => j.Value)).Take(3))
			{
				var mid = $@"
					<tr>
						<td>{HttpUtility.HtmlEncode(player.AccountName)}</td>
						<td>{player.CombinedMechanics["eyes_orb"]}</td>
						<td>{player.CombinedMechanics["eyes_detonate"]}</td>
						<td>{player.CombinedMechanics["eyes_beam"]}</td>
					</tr>";
				top += mid;
			}

			top += "</table>";

			return top;
		}

		public override void Parse(EiLog log, PlayerModel playerModel)
		{
			PrepareParsing(log, playerModel);
			
			var orb = playerModel.Mechanics.GetOrDefault("Light Orb");
			var detonate = playerModel.Mechanics.GetOrDefault("Detonate");
			var beam = playerModel.Mechanics.GetOrDefault("Beam");

			playerModel.CombinedMechanics.Add("eyes_orb", orb);
			playerModel.CombinedMechanics.Add("eyes_detonate", detonate);
			playerModel.CombinedMechanics.Add("eyes_beam", beam);
		}
	}
}