using System.Linq;
using System.Text;
using System.Web;
using raidTimelineLogic.Models;

namespace raidTimelineLogic.HtmlBuilders
{
    internal static class ResurrectionTableBuilder
    {
        internal static StringBuilder BuildResurrectionTable(this StringBuilder stringBuilder, RaidModel raidModel)
        {
            stringBuilder.Append(@"<table class=""resTable"" style=""display: none;"">");

            foreach (var player in raidModel.Players.OrderByDescending(i => i.ResTime).Take(3))
            {
                stringBuilder.Append($@"
					<tr>
						<td>{HttpUtility.HtmlEncode(player.AccountName)}</td> 
						<td title=""Total res: {player.ResTime}"">{player.ResTime}s ({player.ResAmmount}x)</td>
					</tr>");
            }

            stringBuilder.Append("</table>");
            return stringBuilder;
        }
    }
}