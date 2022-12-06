using raidTimeline.Logic.HtmlBuilders;
using raidTimeline.Logic.Models;
using System;
using System.Linq;
using System.Text;

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
			";
        }

        internal static StringBuilder CreateOverviewTableHeader()
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append(@"<div class=""overviewTable statisticsTable""><table><tr>");
            stringBuilder.Append($@"<th class=""overviewTableHeader statisticsTableHeader"">Encounter</th>");
            stringBuilder.Append($@"<th class=""overviewTableHeader statisticsTableHeader"">Failed/Total</th>");
            stringBuilder.Append($@"<th class=""overviewTableHeader statisticsTableHeader"">Time spent</th>");
            stringBuilder.Append($@"<th class=""overviewTableHeader statisticsTableHeader"">Professions</th>");
            stringBuilder.Append("</tr>");

            return stringBuilder;
        }

        internal static StringBuilder CreateOverviewTableEntry(OverviewTableModel tableRow)
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append("<tr>");
            stringBuilder.Append($@"<td><a href=""{tableRow.Url}"" target=""_blank"" style=""color: #aaa; text-decoration: none;"">{tableRow.BossName}</a></td>");
            stringBuilder.Append($"<td>{tableRow.Tries}</td>");
            stringBuilder.Append($"<td>{tableRow.TimePercentage}</td>");
            stringBuilder.Append($@"<td><div class=""overviewIconRow"">");

            var gapCoutner = 0;

            for (int i = 0; i < tableRow.Cells.Count; i++)
            {
                var cell = tableRow.Cells[i];

                if (i != 0 && cell.Group != tableRow.Cells[i - 1].Group)
                {
                    for (int j = gapCoutner; j < 5; j++)
                    {
                        stringBuilder.Append($@"<div class=""horizontalSpacer""></div>");
                    }
                    stringBuilder.Append($@"<div class=""horizontalSpacer""></div>");
                    gapCoutner = 0;
                }

                gapCoutner++;

                stringBuilder.Append($@"<img src=""{cell.ProfessionIcon}"" title=""{cell.AccountName} - {cell.Profession}"" class=""professionIcon"">");
            }

            stringBuilder.Append($"</div></td>");
            stringBuilder.Append("</tr>");

            return stringBuilder;
        }

        internal static StringBuilder CreateOverviewTableFooter()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append("</table></div>");
            return stringBuilder;
        }

        internal static StringBuilder CreateEncounterTableHeader(string[] players)
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append(@"<div class=""encounterTable statisticsTable"" style=""display: none;""><table><tr>");
            stringBuilder.Append($@"<th class=""encounterTableHeader statisticsTableHeader"">Encounter</th>");

            foreach (var player in players)
            {
                stringBuilder.Append($@"<th class=""encounterTableHeader statisticsTableHeader"">{player}</th>");
            }

            stringBuilder.Append("</tr>");

            return stringBuilder;
        }

        internal static StringBuilder CreateEncounterTableFooter()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append("</table></div>");
            return stringBuilder;
        }

        internal static StringBuilder CreateEncounterTableEntry(EncounterTableModel tableRow)
        {
            var stringBuilder = new StringBuilder();
            var killedString = tableRow.Killed ? "killed" : "failed";

            stringBuilder.Append("<tr>");
            stringBuilder.Append($@"<td><a href=""{tableRow.Url}"" target=""_blank"" style=""color: #aaa; text-decoration: none;"">{tableRow.Encounter} ({killedString})</a></td>");

            foreach (var cell in tableRow.Cells.OrderBy(i => i.AccountName))
            {
                if (cell.ProfessionIcon != string.Empty)
                {
                    stringBuilder.Append($@"<td><img src=""{cell.ProfessionIcon}"" title=""{cell.Profession}"" class=""professionIcon""></td>");
                }
                else
                {
                    stringBuilder.Append($@"<td>{cell.Profession}</td>");
                }
            }

            stringBuilder.Append("</tr>");

            return stringBuilder;
        }

        internal static StringBuilder CreateTimelineHeader(this StringBuilder stringBuilder)
        {
            return stringBuilder.Append(@"<div class=""timeline"">");
        }

        internal static StringBuilder CreateTimelineFooter(this StringBuilder stringBuilder)
        {
            return stringBuilder.Append(@"</div>");
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