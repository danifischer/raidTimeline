using System;
using System.Linq;
using System.Text;
using System.Web;
using raidTimelineLogic.Models;

namespace raidTimelineLogic.HtmlBuilders
{
    public static class CrowdControlTableBuilder
    {
        public static StringBuilder BuildCrowdControlTable(this StringBuilder stringBuilder, RaidModel raidModel)
        {
            stringBuilder.Append(@"<table class=""ccTable"" style=""display: none;"">");

            var allCc = raidModel.Players.Sum(i => i.Cc) != 0 ? raidModel.Players.Sum(i => i.Cc) : 1;

            foreach (var player in raidModel.Players.OrderByDescending(i => i.Cc).Take(3))
            {
                stringBuilder.Append($@"
					<tr>
						<td>{HttpUtility.HtmlEncode(player.AccountName)}</td> 
						<td title=""Total CC: {player.Cc}"">{player.Cc} cc</td>
						<td>{Math.Round((double)player.Cc / allCc * 100, 2):F}%</td>
					</tr>");
            }

            stringBuilder.Append("</table>");
            return stringBuilder;
        }
    }
}