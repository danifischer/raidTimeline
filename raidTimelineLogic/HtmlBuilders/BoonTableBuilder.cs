﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using raidTimelineLogic.Models;

namespace raidTimelineLogic.HtmlBuilders
{
    public static class BoonTableBuilder
    {
        public static StringBuilder BuildBoonTable(this StringBuilder stringBuilder, RaidModel raidModel)
        {
            var boonList = CombineBoons(raidModel.Players);
            var counter = 0;

            while (boonList.Skip(counter).Take(6).Any())
            {
                stringBuilder.Append(@"<table class=""boonTable"" style=""display: none;"">");

                var boonHeader = @"<tr>";
                var boonBody = @"<tr>";

                foreach (var boon in boonList.Skip(counter).Take(6))
                {
                    boonHeader += $@"
					<th class=""boonColumn"">
						<img src=""{boon.Icon}"" class=""boonIcon"" title=""{boon.Name}"">
					</th>";

                    if (boon.Stacking && boon.Stacks != null)
                    {
                        boonBody += $@"
						<td class=""boonColumn"" title=""{Math.Round((double)boon.Stacks, 0)}%"">
							{Math.Round(boon.Value, 0)}
						</td>";
                    }
                    else
                    {
                        boonBody += $@"
						<td class=""boonColumn"">
							{Math.Round(boon.Value, 0)}%
						</td>";
                    }
                }

                boonHeader += @"</tr>";
                boonBody += @"</tr>";

                stringBuilder.Append(boonHeader);
                stringBuilder.Append(boonBody);

                stringBuilder.Append("</table>");
                counter += 6;
            }
            return stringBuilder;
        }
        
        private static List<BuffModel> CombineBoons(IReadOnlyCollection<PlayerModel> players)
        {
            var boonList = new List<BuffModel>();
            var groupedBoons = players.SelectMany(i => i.Buffs).GroupBy(b => b.Id);
            foreach (var boon in groupedBoons)
            {
                var model = new BuffModel();

                var boonInfo = boon.First();
                model.Id = boonInfo.Id;
                model.Name = boonInfo.Name;
                model.Icon = boonInfo.Icon;
                model.Stacking = boonInfo.Stacking;
                model.Value = boon.Sum(i => i.Value) / players.Count;
                model.Stacks = boon.Sum(i => i.Stacks) / players.Count;

                boonList.Add(model);
            }

            return boonList;
        }
    }
}