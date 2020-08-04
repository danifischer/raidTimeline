using raidTimelineLogic.Models;
using System;
using System.Linq;
using System.Web;

namespace raidTimelineLogic
{
	internal static class HtmlCreator
	{
		public static string CreateHeaderHtml(DateTime key, int killed, int failed, TimeSpan tryTime, TimeSpan raidTime, int bosses)
		{
			return $@"
				<div class=""header noselect"">
					<div class=""box"">
						<div class=""text"">
							<h2 style=""text-align: center; margin-top: 15px; margin-bottom: 5px;"">{key.ToShortDateString()}</h2>
							<div style=""padding: 5px;"">
								 <img src=""https://wiki.guildwars2.com/images/2/20/Event_star_red_%28map_icon%29.png""
									style=""position: relative; top: 12px; left: 3px; height: 32px; width: 32px;"" title=""Total Raid time"">
									<b>Total:</b> {raidTime.Hours}h {raidTime.Minutes}m {raidTime.Seconds}s
								 <img src=""https://wiki.guildwars2.com/images/d/d4/Casino_Blitz_%28map_icon%29.png""
									style=""position: relative; top: 12px; left: 3px; height: 32px; width: 32px;"" title=""Fight time"">
									<b>Fight:</b> {tryTime.Hours}h {tryTime.Minutes}m {tryTime.Seconds}s
							 </div>
							<div style=""padding: 5px;"">
								<img src=""https://wiki.guildwars2.com/images/thumb/b/b0/Red_Boss.png/20px-Red_Boss.png"" style=""position: relative; top: 4px; left: 0px;""
									title=""# of different bosses"">
									{bosses}
								<img src=""https://wiki.guildwars2.com/images/5/52/Tick_green.png"" style=""position: relative; top: 4px; left: 0px;""
									title=""# of kills"">
									{killed}
								<img src=""https://wiki.guildwars2.com/images/4/46/Cross_red.png"" style=""position: relative; top: 4px; left: 0px;""
									title=""# of fails"">
									{failed}
							</div>
						</div>
					</div>
				</div>
				<div class=""timeline"">
			";
		}

		public static string CreateEncounterHtmlPass(RaidModel model)
		{
			var top = $@"<div class=""container left"">";
			return top + CommonHtml(model);
		}

		public static string CreateEncounterHtmlFail(RaidModel model)
		{
			var top = $@"<div class=""container right"">";
			return top + CommonHtml(model);
		}

		private static string CommonHtml(RaidModel model)
		{
			var encounterTime = model.OccurenceEnd - model.OccurenceStart;

			var top = $@"
				<div class=""content"">
					<a href=""{model.LogUrl}"" target=""_blank"" style=""color: #aaa; text-decoration: none;"">
						<img src=""{model.EncounterIcon}"" alt=""{HttpUtility.HtmlEncode(model.EncounterName)}"" width=""64"" height=""64"" style=""float: right;"">
						<h2>{HttpUtility.HtmlEncode(model.EncounterName)}</h2>
						<p>{model.OccurenceStart.ToLongTimeString()} &rArr; {model.OccurenceEnd.ToLongTimeString()} ({encounterTime.Minutes}m {encounterTime.Seconds}s)
					</a>
					";

			foreach (var value in model.HpLeft)
			{
				var mid = $@"
					<div title=""{model.DoubleAsHtml(value)}% left"" style=""background: rgba(0, 0, 0, 0)
						linear-gradient(to right, red {model.DoubleAsHtml(value)}%, {model.DoubleAsHtml(value)}%, green {model.DoubleAsHtml(100 - value)}%)
						repeat scroll 0% 0%; height: 10px; width: 100%; border-radius: 5px; margin-top: 3px;"">
					</div>";
				top += mid;
			}

			top += @"<table class=""dpsTable"">";

			var allDamage = model.Players.Sum(i => i.Damage) != 0 ? model.Players.Sum(i => i.Damage) : 1;

			foreach (var player in model.Players.OrderByDescending(i => i.Dps).Take(3))
			{
				var mid = $@"
					<tr style=""color: #aaa"">
						<td>{HttpUtility.HtmlEncode(player.AccountName)}</td> 
						<td title=""Total Damage: {player.Damage}"">{player.Dps} dps</td>
						<td>{Math.Round((double)player.Damage / allDamage * 100, 2):F}%</td>
					</tr>";
				top += mid;
			}

			top += "</table>";

			var bot = $@"
				</div>
			</div>
			";
			top += bot;

			return top;
		}
	}
}