using raidTimelineLogic.Helper;
using raidTimelineLogic.Models;
using System.Linq;
using System.Web;

namespace raidTimelineLogic.Mechanics.Strategies
{
	internal class CardinalAdinaMechanics : BaseMechanics
	{
		public CardinalAdinaMechanics()
		{
			EncounterIcon = "https://wiki.guildwars2.com/images/a/a0/Mini_Earth_Djinn.png";
		}

		public override string CreateHtml(RaidModel model)
		{
			var top = "";
			top += @"<table class=""mechanicsTable"" style=""display: none;"">";
			top += @"<tr style=""color: #aaa"">
						<th>Player</th>
						<th title=""Unremovable blindness"">Blind</th>
						<th title=""Stacking damage debuff from Hand of Erosion"">Curse</th>
						<th title=""Perilous Pulse"">Pulse</th>
						<th title=""Hit by mines"">Mines</th>
						<th title=""Looked at Eye"">Eye</th>
					</tr>";

			foreach (var player in model.Players.OrderByDescending(i => i.Mechanics.Sum(j => j.Value)).Take(3))
			{
				var mid = $@"
					<tr style=""color: #aaa"">
						<td>{HttpUtility.HtmlEncode(player.AccountName)}</td>
						<td>{player.Mechanics["adina_blind"]}</td>
						<td>{player.Mechanics["adina_curse"]}</td>
						<td>{player.Mechanics["adina_pulse"]}</td>
						<td>{player.Mechanics["adina_mines"]}</td>
						<td>{player.Mechanics["adina_eye"]}</td>
					</tr>";
				top += mid;
			}

			top += "</table>";

			return top;
		}

		public override void Parse(dynamic logData, PlayerModel playerModel)
		{
			PrepareParsing(logData, playerModel);

			var blind = playerModel.Mechanics.GetOrDefault("R.Blind");
			var curse = playerModel.Mechanics.GetOrDefault("Curse");
			var pulse = playerModel.Mechanics.GetOrDefault("Perilous Pulse");
			var mines = playerModel.Mechanics.GetOrDefault("Mines");
			var eye = playerModel.Mechanics.GetOrDefault("Eye");

			playerModel.Mechanics.Add("adina_blind", blind);
			playerModel.Mechanics.Add("adina_curse", curse);
			playerModel.Mechanics.Add("adina_pulse", pulse);
			playerModel.Mechanics.Add("adina_mines", mines);
			playerModel.Mechanics.Add("adina_eye", eye);
		}
	}
}