﻿using System;
using System.Linq;
using System.Text;
using System.Web;
using raidTimelineLogic.Models;

namespace raidTimelineLogic.HtmlBuilders
{
    public static class DamagePerSecondTableBuilder
    {
        public static StringBuilder BuildDamagePerSecondTable(this StringBuilder stringBuilder, RaidModel raidModel)
        {
            stringBuilder.Append(@"<table class=""dpsTable"">");

            var allDamage = raidModel.Players.Sum(i => i.Damage) != 0 ? raidModel.Players.Sum(i => i.Damage) : 1;

            foreach (var player in raidModel.Players.OrderByDescending(i => i.Dps).Take(3))
            {
                stringBuilder.Append($@"
					<tr>
						<td>{HttpUtility.HtmlEncode(player.AccountName)}</td> 
						<td title=""Total Damage: {player.Damage}"">{player.Dps} dps</td>
						<td>{Math.Round((double)player.Damage / allDamage * 100, 2):F}%</td>
					</tr>");
            }

            stringBuilder.Append("</table>");
            return stringBuilder;
        }
    }
}