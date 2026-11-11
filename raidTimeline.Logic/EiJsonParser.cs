#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using raidTimeline.Logic.EiJson;
using raidTimeline.Logic.Interfaces;
using raidTimeline.Logic.Mechanics;
using raidTimeline.Models.Models;

[assembly: InternalsVisibleTo("raidTimeline.Logic.Tests")]

namespace raidTimeline.Logic
{
    /// <summary>
    /// Creates <see cref="RaidModel"/>s from the JSON output of Elite Insights (3.x), either from a local file
    /// or from the getJson endpoint of dps.report.
    /// </summary>
    internal static class EiJsonParser
    {
        private const string DateFormat = "yyyy-MM-dd HH:mm:ss zzz";

        private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

        // Display order of the boon table; boons Elite Insights reports beyond these are appended.
        private static readonly int[] BoonOrder =
        {
            740, // Might
            725, // Fury
            1187, // Quickness
            30328, // Alacrity
            717, // Protection
            718, // Regeneration
            726, // Vigor
            743, // Aegis
            1122, // Stability
            719, // Swiftness
            26980, // Resistance
            873 // Retaliation
        };

        private static readonly MechanicsFactory MechanicsFactory = MechanicsFactory.GetMechanicsFactory();

        /// <summary>
        /// Parses an Elite Insights json file.
        /// </summary>
        /// <param name="filePath">Path to the json file.</param>
        /// <param name="logger">Optional logger.</param>
        /// <param name="logUrl">Link to the human readable log. Defaults to the html file Elite Insights writes next to the json.</param>
        /// <returns>The parsed model, or <c>null</c> if the file is not a parsable log.</returns>
        internal static RaidModel? ParseFile(string filePath, ILogger? logger = null, string? logUrl = null)
        {
            try
            {
                using var stream = File.OpenRead(filePath);
                var log = JsonSerializer.Deserialize<EiLog>(stream, Options);
                return Build(log, filePath, logUrl, logger);
            }
            catch (Exception e) when (e is JsonException or IOException or UnauthorizedAccessException
                                          or FormatException or ArgumentException)
            {
                logger?.LogError("{FilePath} cannot be parsed: {Message}", filePath, e.Message);
                return null;
            }
        }

        /// <summary>
        /// Parses Elite Insights json from memory (e.g. the response of dps.report).
        /// </summary>
        /// <param name="json">The json text.</param>
        /// <param name="source">Name used as <see cref="RaidModel.LogPath"/>.</param>
        /// <param name="logUrl">Link to the human readable log.</param>
        /// <param name="logger">Optional logger.</param>
        internal static RaidModel? ParseJson(string json, string source, string logUrl, ILogger? logger = null)
        {
            try
            {
                var log = JsonSerializer.Deserialize<EiLog>(json, Options);
                return Build(log, source, logUrl, logger);
            }
            catch (Exception e) when (e is JsonException or FormatException or ArgumentException)
            {
                logger?.LogError("{Source} cannot be parsed: {Message}", source, e.Message);
                return null;
            }
        }

        private static RaidModel? Build(EiLog? log, string source, string? logUrl, ILogger? logger)
        {
            if (log == null || log.Players.Count == 0 || log.Phases.Count == 0)
            {
                logger?.LogError("{Source} does not contain Elite Insights log data.", source);
                return null;
            }

            var model = new RaidModel(source)
            {
                EncounterName = log.FightName,
                EncounterIcon = log.FightIcon,
                EncounterId = log.TriggerID,
                EncounterTime = log.Duration,
                Killed = log.Success,
                OccurenceStart = ParseDate(log.TimeStartStd),
                OccurenceEnd = ParseDate(log.TimeEndStd)
            };

            model.LogUrl = logUrl ?? Path.GetFileName(Path.ChangeExtension(source, ".html"));

            var mainTargets = GetMainTargets(log);
            foreach (var target in mainTargets)
            {
                model.HpLeft.Add(Math.Round(100 - log.Targets[target].HealthPercentBurned, 2));
            }

            var strategy = MechanicsFactory.FindStrategy(log.TriggerID);
            var boons = GetBoons(log);

            for (var index = 0; index < log.Players.Count; index++)
            {
                var player = log.Players[index];
                if (player.FriendlyNPC) continue;

                // Index stays the position in the log, the mechanics read the log by it.
                var playerModel = new PlayerModel
                {
                    Index = index,
                    AccountName = player.Account,
                    Profession = player.Profession,
                    ProfessionIcon = ProfessionIcons.For(player.Profession),
                    IsNpc = player.NotInSquad,
                    Group = player.Group
                };

                ParseDamage(playerModel, player, mainTargets, log.DurationMS);
                ParseSupport(playerModel, player);
                ParseDefenses(playerModel, player);
                ParseBoons(playerModel, player, boons);
                ParseMechanics(playerModel, strategy, log, logger, source);

                model.Players.Add(playerModel);
            }

            return model;
        }

        private static DateTime ParseDate(string value) =>
            DateTime.ParseExact(value, DateFormat, CultureInfo.InvariantCulture);

        /// <summary>Targets of the full fight which count as the boss (not blocking adds or secondary targets).</summary>
        private static List<int> GetMainTargets(EiLog log)
        {
            var phase = log.Phases[0];
            var main = phase.Targets
                .Where(t => t >= 0 && t < log.Targets.Count)
                .Where(t => phase.TargetPriorities.Count == 0
                            || (phase.TargetPriorities.TryGetValue(t.ToString(CultureInfo.InvariantCulture), out var priority)
                                && priority == "MAIN"))
                .ToList();

            return main.Count > 0 ? main : Enumerable.Range(0, Math.Min(1, log.Targets.Count)).ToList();
        }

        private static void ParseDamage(PlayerModel playerModel, EiPlayer player, List<int> targets, long durationMs)
        {
            playerModel.Damage = targets
                .Where(t => t < player.DpsTargets.Count && player.DpsTargets[t].Count > 0)
                .Sum(t => player.DpsTargets[t][0].Damage);
            playerModel.Dps = durationMs > 0 ? playerModel.Damage * 1000 / durationMs : 0;
            playerModel.Cc = player.DpsAll.Count > 0 ? (long)player.DpsAll[0].BreakbarDamage : 0;
        }

        private static void ParseSupport(PlayerModel playerModel, EiPlayer player)
        {
            if (player.Support.Count == 0) return;

            playerModel.ResAmount = player.Support[0].Resurrects;
            playerModel.ResTime = player.Support[0].ResurrectTime;
        }

        private static void ParseDefenses(PlayerModel playerModel, EiPlayer player)
        {
            if (player.Defenses.Count == 0) return;

            playerModel.TimesDowned = player.Defenses[0].DownCount;
            playerModel.TimesDied = player.Defenses[0].DeadCount;
        }

        private static List<(int Id, EiBuffDescription Description)> GetBoons(EiLog log)
        {
            var boons = new List<(int Id, EiBuffDescription Description)>();
            foreach (var (key, description) in log.BuffMap)
            {
                // Keys of the buff map look like "b740".
                if (description.Classification == "Boon" && key.Length > 1 && key[0] == 'b'
                    && int.TryParse(key.AsSpan(1), NumberStyles.None, CultureInfo.InvariantCulture, out var id))
                {
                    boons.Add((id, description));
                }
            }

            static int Rank(int id)
            {
                var position = Array.IndexOf(BoonOrder, id);
                return position >= 0 ? position : BoonOrder.Length + id;
            }

            return boons.OrderBy(b => Rank(b.Id)).ToList();
        }

        private static void ParseBoons(PlayerModel playerModel, EiPlayer player,
            List<(int Id, EiBuffDescription Description)> boons)
        {
            for (var i = 0; i < boons.Count; i++)
            {
                var (id, description) = boons[i];
                var data = player.BuffUptimes.FirstOrDefault(b => b.Id == id)?.BuffData.FirstOrDefault();

                playerModel.Buffs.Add(new BuffModel
                {
                    Id = id.ToString(CultureInfo.InvariantCulture),
                    Name = description.Name,
                    Icon = description.Icon,
                    Stacking = description.Stacking,
                    Index = i,
                    Value = data?.Uptime ?? 0,
                    Stacks = description.Stacking && data != null ? data.Presence : null
                });
            }
        }

        private static void ParseMechanics(PlayerModel playerModel, IMechanics? strategy, EiLog log,
            ILogger? logger, string source)
        {
            if (strategy == null) return;

            try
            {
                strategy.Parse(log, playerModel);
            }
            catch (Exception e)
            {
                logger?.LogDebug(e, "Mechanics not parsable: {Source}", source);
            }
        }
    }
}
