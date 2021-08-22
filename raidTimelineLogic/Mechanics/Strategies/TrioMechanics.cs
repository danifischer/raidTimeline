﻿using raidTimelineLogic.Helper;
using raidTimelineLogic.Models;
using System.Linq;
using System.Web;

namespace raidTimelineLogic.Mechanics.Strategies
{
	internal class TrioMechanics : BaseMechanics
	{
		public TrioMechanics()
		{
			EncounterIcon = "https://i.imgur.com/UZZQUdf.png";
		}
		
		public override string CreateHtml(RaidModel model)
		{
			var top = "";
			top += @"<table class=""mechanicsTable"" style=""display: none;"">";
			top += @"<tr style=""color: #aaa"">
						<th>Player</th>
						<th title=""Hail of Bullets (Zane Cone Shot)"">Zane Cone</th>
						<th title=""Fiery Vortex (Tornado)"">Tornado</th>
					</tr>";

			foreach (var player in model.Players.OrderByDescending(i => i.Mechanics.Sum(j => j.Value)).Take(3))
			{
				var mid = $@"
					<tr style=""color: #aaa"">
						<td>{HttpUtility.HtmlEncode(player.AccountName)}</td>
						<td>{player.Mechanics["trio_cone"]}</td>
						<td>{player.Mechanics["trio_tornado"]}</td>
					</tr>";
				top += mid;
			}

			top += "</table>";

			return top;
		}

		public override void Parse(dynamic logData, PlayerModel playerModel)
		{
			PrepareParsing(logData, playerModel);

			var cone = playerModel.Mechanics.GetOrDefault("Zane Cone");
			var tornado = playerModel.Mechanics.GetOrDefault("Tornado");

			playerModel.Mechanics.Add("trio_cone", cone);
			playerModel.Mechanics.Add("trio_tornado", tornado);
		}
	}
}