using System;
using System.Text;
using raidTimeline.Logic.Models;
using raidTimeline.Logic.HtmlBuilders;

namespace raidTimeline.Logic
{
	internal static class HtmlCreator
	{
		internal static string CreateHeaderHtml(DateTime key, int killed, int failed, TimeSpan tryTime, TimeSpan raidTime, int bosses)
		{
			var downTime = raidTime - tryTime;

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
							     <img src=""https://wiki.guildwars2.com/images/c/c6/Downed_enemy.png""
									style=""position: relative; top: 12px; left: 3px; height: 32px; width: 32px;"" title=""Down time"">
									<b>Down:</b> {downTime.Hours}h {downTime.Minutes}m {downTime.Seconds}s
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

		internal static StringBuilder CreateEncounterHtmlPass(RaidModel model)
		{
			var stringBuilder = 
				new StringBuilder($@"<div class=""container left"">")
					.BuildCommonHtml(model);
			
			return stringBuilder;
		}

		internal static StringBuilder CreateEncounterHtmlFail(RaidModel model)
		{
			var stringBuilder = 
				new StringBuilder($@"<div class=""container right"">")
				.BuildCommonHtml(model);
			
			return stringBuilder;
		}

		private static StringBuilder BuildCommonHtml(this StringBuilder stringBuilder, RaidModel model)
		{
			return stringBuilder.BuildEncounterHeader(model)
				.BuildDamagePerSecondTable(model)
				.BuildCrowdControlTable(model)
				.BuildResurrectionTable(model)
				.BuildBoonTable(model)
				.BuildMechanicsTable(model)
				.BuildEncounterFooter();
		}
	}
}