using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using raidTimeline.Models.Models;

namespace raidTimeline.Logic
{
    /// <summary>
    /// Turns the parsed encounters into the json the timeline page renders in the browser.
    /// The page (<c>template.html</c>) groups the logs by day, computes downtimes and statistics and draws
    /// everything, so the layout can change without touching any C# code.
    /// </summary>
    internal static class TimelineDataBuilder
    {
        private const string DateFormat = "yyyy-MM-dd'T'HH:mm:ss";

        // The default encoder escapes <, > and & as \uXXXX, so the json can never end the surrounding script tag.
        private static readonly JsonSerializerOptions Options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        /// <param name="models">The encounters, in any order.</param>
        /// <param name="newestFirst">Whether the page lists the newest raid day first.</param>
        internal static string Build(IEnumerable<RaidModel> models, bool newestFirst)
        {
            var logs = models.OrderBy(model => model.OccurenceStart).Select(ToLog).ToList();
            return JsonSerializer.Serialize(new TimelineData(newestFirst, logs), Options);
        }

        private static LogData ToLog(RaidModel model) => new(
            model.EncounterName,
            model.EncounterIcon,
            model.EncounterId,
            model.Killed,
            model.OccurenceStart.ToString(DateFormat, CultureInfo.InvariantCulture),
            model.OccurenceEnd.ToString(DateFormat, CultureInfo.InvariantCulture),
            model.LogUrl,
            model.HpLeft,
            model.MechanicColumns,
            model.Players.Select(ToPlayer).ToList());

        private static PlayerData ToPlayer(PlayerModel player) => new(
            player.AccountName,
            player.Profession,
            player.ProfessionIcon,
            player.Group,
            player.IsNpc,
            player.Damage,
            player.Dps,
            player.Cc,
            player.ResAmount,
            player.ResTime,
            player.TimesDowned,
            player.TimesDied,
            player.CombinedMechanics,
            player.Buffs.Select(buff => new BuffData(buff.Id, buff.Name, buff.Icon, buff.Stacking, buff.Value, buff.Stacks)).ToList());

        private sealed record TimelineData(bool NewestFirst, IReadOnlyList<LogData> Logs);

        private sealed record LogData(
            string Name, string Icon, int Id, bool Killed, string Start, string End, string Url,
            IReadOnlyList<double> HpLeft, IReadOnlyList<MechanicColumn> Columns, IReadOnlyList<PlayerData> Players);

        private sealed record PlayerData(
            string Account, string Profession, string Icon, int Group, bool IsNpc,
            long Damage, long Dps, long Cc, int ResAmount, double ResTime, int Downed, int Died,
            IReadOnlyDictionary<string, int> Mechanics, IReadOnlyList<BuffData> Buffs);

        private sealed record BuffData(string Id, string Name, string Icon, bool Stacking, double Value, double? Stacks);
    }
}
