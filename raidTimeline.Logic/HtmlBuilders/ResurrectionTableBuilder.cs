using System.Linq;
using System.Text;
using System.Web;
using raidTimeline.Logic.Models;

namespace raidTimeline.Logic.HtmlBuilders
{
    internal static class ResurrectionTableBuilder
    {
        internal static StringBuilder BuildResurrectionTable(this StringBuilder stringBuilder, RaidModel raidModel)
        {
            stringBuilder.Append(@"<table class=""resTable"" style=""display: none;"">");

            foreach (var player in raidModel.Players.OrderByDescending(i => i.ResAmount).Take(3))
            {
                stringBuilder.Append($@"
					<tr>
						<td>{HttpUtility.HtmlEncode(player.AccountName)}</td> 
						<td title=""Total res: {player.ResTime}"">{player.ResTime}s - {player.ResAmount}x</td>
					</tr>");
            }

            stringBuilder.Append("</table>");
            return stringBuilder;
        }
    }
}