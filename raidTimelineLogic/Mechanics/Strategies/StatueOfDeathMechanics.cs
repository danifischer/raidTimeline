using raidTimelineLogic.Models;
using System.Linq;
using System.Web;

namespace raidTimelineLogic.Mechanics.Strategies
{
	internal class StatueOfDeathMechanics : IMechanics
	{
		internal readonly string EncounterIcon = "https://wiki.guildwars2.com/images/thumb/2/24/Eater_of_Souls_%28Hall_of_Chains%29.jpg/194px-Eater_of_Souls_%28Hall_of_Chains%29.jpg";

		public string CreateHtml(RaidModel model)
		{
			var top = "";
			top += @"<table class=""mechanicsTable"" style=""display: none;"">";
			top += @"<tr style=""color: #aaa"">
						<th>Player</th>
						<th title=""Hungering Miasma (Vomit Goo)"">Vomit</th>
						<th title=""Applied when taking green"">Orb CD</th>
					</tr>";

			foreach (var player in model.Players.OrderByDescending(i => i.Mechanics.Where(k => k.Key == "souleater_vomit").Sum(j => j.Value)).Take(3))
			{
				var mid = $@"
					<tr style=""color: #aaa"">
						<td>{HttpUtility.HtmlEncode(player.AccountName)}</td>
						<td>{player.Mechanics["souleater_vomit"]}</td>
						<td>{player.Mechanics["souleater_orbCd"]}</td>
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

			var vomit = (int)mechanics[0][0];
			var orbCd = (int)mechanics[1][0];
			
			playerModel.Mechanics.Add("souleater_vomit", vomit);
			playerModel.Mechanics.Add("souleater_orbCd", orbCd);
		}
	}
}