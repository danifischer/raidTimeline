﻿using raidTimelineLogic.Helper;
using raidTimelineLogic.Models;
using System.Linq;
using System.Web;

namespace raidTimelineLogic.Mechanics.Strategies
{
	internal class StatueOfIceMechanics : BaseMechanics
	{
		public StatueOfIceMechanics()
		{
			EncounterIcon = "https://wiki.guildwars2.com/images/3/37/Mini_Broken_King.png";
		}

		public override string CreateHtml(RaidModel model)
		{
			var top = "";
			top += @"<table class=""mechanicsTable"" style=""display: none;"">";
			top += @"<tr style=""color: #aaa"">
						<th>Player</th>
						<th title=""King's Wrath (Auto Attack Cone Part)"">Cone</th>
						<th title=""Numbing Breach (Ice Cracks in the Ground)"">Cracks</th>
						<th title=""Frozen Wind (Stood in Green)"">Green</th>
					</tr>";

			foreach (var player in model.Players.OrderByDescending(i => i.Mechanics.Where(k => k.Key == "brokenking_cracks").Sum(j => j.Value)).Take(3))
			{
				var mid = $@"
					<tr style=""color: #aaa"">
						<td>{HttpUtility.HtmlEncode(player.AccountName)}</td>
						<td>{player.Mechanics["brokenking_cone"]}</td>
						<td>{player.Mechanics["brokenking_cracks"]}</td>
						<td>{player.Mechanics["brokenking_green"]}</td>
					</tr>";
				top += mid;
			}

			top += "</table>";

			return top;
		}

		public override void Parse(dynamic logData, PlayerModel playerModel)
		{
			PrepareParsing(logData, playerModel);

			var cone = playerModel.Mechanics.GetOrDefault("Cone Hit");
			var cracks = playerModel.Mechanics.GetOrDefault("Cracks");
			var green = playerModel.Mechanics.GetOrDefault("Green");

			playerModel.Mechanics.Add("brokenking_cone", cone);
			playerModel.Mechanics.Add("brokenking_cracks", cracks);
			playerModel.Mechanics.Add("brokenking_green", green);
		}
	}
}