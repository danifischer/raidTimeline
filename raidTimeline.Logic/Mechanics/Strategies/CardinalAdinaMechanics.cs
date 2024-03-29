﻿using raidTimeline.Logic.Helper;
using System.Linq;
using System.Web;
using raidTimeline.Logic.Models;

namespace raidTimeline.Logic.Mechanics.Strategies
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
			top += @"<tr>
						<th>Player</th>
						<th title=""Unremovable blindness"">Blind</th>
						<th title=""Stacking damage debuff from Hand of Erosion"">Curse</th>
						<th title=""Perilous Pulse"">Pulse</th>
						<th title=""Hit by mines"">Mines</th>
						<th title=""Looked at Eye"">Eye</th>
					</tr>";

			foreach (var player in model.Players.OrderByDescending(i => i.CombinedMechanics.Sum(j => j.Value)).Take(3))
			{
				var mid = $@"
					<tr>
						<td>{HttpUtility.HtmlEncode(player.AccountName)}</td>
						<td>{player.CombinedMechanics["adina_blind"]}</td>
						<td>{player.CombinedMechanics["adina_curse"]}</td>
						<td>{player.CombinedMechanics["adina_pulse"]}</td>
						<td>{player.CombinedMechanics["adina_mines"]}</td>
						<td>{player.CombinedMechanics["adina_eye"]}</td>
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

			playerModel.CombinedMechanics.Add("adina_blind", blind);
			playerModel.CombinedMechanics.Add("adina_curse", curse);
			playerModel.CombinedMechanics.Add("adina_pulse", pulse);
			playerModel.CombinedMechanics.Add("adina_mines", mines);
			playerModel.CombinedMechanics.Add("adina_eye", eye);
		}
	}
}