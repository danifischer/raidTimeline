using raidTimelineLogic.Helper;
using raidTimelineLogic.Models;
using System.Linq;
using System.Web;

namespace raidTimelineLogic.Mechanics.Strategies
{
	internal class StatueOfDarknessMechanics : BaseMechanics
	{
		public StatueOfDarknessMechanics()
		{
			EncounterIcon = "https://wiki.guildwars2.com/images/thumb/a/a7/Eye_of_Fate.jpg/188px-Eye_of_Fate.jpg";
		}

		public override string CreateHtml(RaidModel model)
		{
			var top = "";
			top += @"<table class=""mechanicsTable"" style=""display: none;"">";
			top += @"<tr style=""color: #aaa"">
						<th>Player</th>
						<th title=""Light Carrier (picked up a light orb)"">Light Orb</th>
						<th title=""Flare (detonate light orb to incapacitate eye)"">Detonate</th>
						<th title=""Deep Abyss (ticking eye beam)"">Beam</th>
					</tr>";

			foreach (var player in model.Players.OrderByDescending(i => i.CombinedMechanics.Where(k => k.Key == "eyes_beam").Sum(j => j.Value)).Take(3))
			{
				var mid = $@"
					<tr style=""color: #aaa"">
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

		public override void Parse(dynamic logData, PlayerModel playerModel)
		{
			PrepareParsing(logData, playerModel);
			
			var orb = playerModel.Mechanics.GetOrDefault("Light Orb");
			var detonate = playerModel.Mechanics.GetOrDefault("Detonate");
			var beam = playerModel.Mechanics.GetOrDefault("Beam");

			playerModel.CombinedMechanics.Add("eyes_orb", orb);
			playerModel.CombinedMechanics.Add("eyes_detonate", detonate);
			playerModel.CombinedMechanics.Add("eyes_beam", beam);
		}
	}
}