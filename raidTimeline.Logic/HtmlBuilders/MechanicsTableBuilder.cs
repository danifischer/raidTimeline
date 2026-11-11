using System.Linq;
using System.Text;
using System.Web;
using raidTimeline.Models.Models;

namespace raidTimeline.Logic.HtmlBuilders
{
    internal static class MechanicsTableBuilder
    {
        private const int PlayersShown = 3;

        /// <summary>
        /// Shows the players with the most mechanic fails of the encounter. The columns come from
        /// <see cref="RaidModel.MechanicColumns"/>, nothing is added if there are none.
        /// </summary>
        internal static StringBuilder BuildMechanicsTable(this StringBuilder stringBuilder, RaidModel raidModel)
        {
            var columns = raidModel.MechanicColumns;
            if (columns.Count == 0)
            {
                return stringBuilder;
            }

            int Value(PlayerModel player, MechanicColumn column) =>
                player.CombinedMechanics.TryGetValue(column.Key, out var value) ? value : 0;

            var rankColumns = columns.Where(c => c.Rank).ToList();
            var worst = raidModel.Players
                .OrderByDescending(player => rankColumns.Sum(column => Value(player, column)))
                .Take(PlayersShown);

            stringBuilder.Append(@"<table class=""mechanicsTable"" style=""display: none;"">");
            stringBuilder.Append("<tr><th>Player</th>");
            foreach (var column in columns)
            {
                stringBuilder.Append(
                    $@"<th title=""{HttpUtility.HtmlEncode(column.Title)}"">{HttpUtility.HtmlEncode(column.Label)}</th>");
            }

            stringBuilder.Append("</tr>");

            foreach (var player in worst)
            {
                stringBuilder.Append($"<tr><td>{HttpUtility.HtmlEncode(player.AccountName)}</td>");
                foreach (var column in columns)
                {
                    stringBuilder.Append($"<td>{Value(player, column)}</td>");
                }

                stringBuilder.Append("</tr>");
            }

            stringBuilder.Append("</table>");
            return stringBuilder;
        }
    }
}
