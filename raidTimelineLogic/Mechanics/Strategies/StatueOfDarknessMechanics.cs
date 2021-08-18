using raidTimelineLogic.Models;
using System.Linq;
using System.Web;

namespace raidTimelineLogic.Mechanics.Strategies
{
	internal class StatueOfDarknessMechanics : IMechanics
	{
		internal readonly string EncounterIcon = "https://wiki.guildwars2.com/images/thumb/a/a7/Eye_of_Fate.jpg/188px-Eye_of_Fate.jpg";

		public string CreateHtml(RaidModel model)
		{
			var top = "";
			top += @"<table class=""mechanicsTable"" style=""display: none;"">";
			top += @"<tr style=""color: #aaa"">
						<th>Player</th>
						<th title=""Light Carrier (picked up a light orb)"">Light Orb</th>
						<th title=""Flare (detonate light orb to incapacitate eye)"">Detonate</th>
						<th title=""Deep Abyss (ticking eye beam)"">Beam</th>
					</tr>";

			foreach (var player in model.Players.OrderByDescending(i => i.Mechanics.Where(k => k.Key == "eyes_beam").Sum(j => j.Value)).Take(3))
			{
				var mid = $@"
					<tr style=""color: #aaa"">
						<td>{HttpUtility.HtmlEncode(player.AccountName)}</td>
						<td>{player.Mechanics["eyes_orb"]}</td>
						<td>{player.Mechanics["eyes_detonate"]}</td>
						<td>{player.Mechanics["eyes_beam"]}</td>
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

			var orb = (int)mechanics[0][0];
			var detonate = (int)mechanics[1][0];
			var beam = (int)mechanics[2][0];

			playerModel.Mechanics.Add("eyes_orb", orb);
			playerModel.Mechanics.Add("eyes_detonate", detonate);
			playerModel.Mechanics.Add("eyes_beam", beam);
		}
	}
}