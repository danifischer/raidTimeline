using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using OneOf;
using OneOf.Types;
using raidTimeline.Logic.Mechanics;
using raidTimeline.Models.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("raidTimeline.Logic.Tests")]

namespace raidTimeline.Logic
{
    internal static class EiHtmlParser
    {
        private static readonly MechanicsFactory MechanicsFactory = MechanicsFactory.GetMechanicsFactory();

        internal static RaidModel ParseLog(string filePath, ILogger logger = null)
        {
            var model = new RaidModel(filePath);

            try
            {
                var loadResult = LoadLogData(model.LogPath);
                if (loadResult.IsT1)
                {
                    return null;
                }

                var logData = loadResult.AsT0;

                SetRemainingHealth(model, logData);
                SetGeneralInformation(model, logData);
                if (logData["eiVersion"] != null || logData["parser"] != null)
                {
                    model.OccurenceStart = logData["encounterStart"].Value<DateTime>();
                    model.OccurenceEnd = logData["encounterEnd"].Value<DateTime>();
                }

                var fightDuration = logData["phases"][0]["duration"].Value<long>();

                for (var j = 0; j < logData["players"].Count(); j++)
                {
                    var player = logData["players"][j];
                    var playerModel = new PlayerModel
                    {
                        Index = j,
                        AccountName = player["acc"].Value<string>(),
                        Profession = player["profession"].Value<string>(),
                        ProfessionIcon = player["icon"].Value<string>(),
                        IsNpc = player["notInSquad"].Value<bool>(),
                        Group = player["group"].Value<int>()
                    };

                    ParseSupportStats(playerModel, logData);
                    ParseDamageStats(playerModel, fightDuration, logData);
                    ParseMechanics(playerModel, model, logData, logger);
                    ParseBoons(playerModel, logData);
                    ParseDefStats(playerModel, logData);

                    model.Players.Add(playerModel);
                }
            }
            catch (Exception e)
            {
                logger?.LogError($">>> {filePath} cannot be parsed.");
                logger?.LogDebug(e.Message);
                return null;
            }

            return model;
        }

        private static void ParseBoons(this PlayerModel playerModel, JObject logData)
        {
            var boons = logData["boons"];
            var boonList = new List<string>();

            foreach (var boon in boons)
            {
                boonList.Add(boon.Value<string>());
            }

            var maps = new List<BuffModel>();
            var map = logData["buffMap"];

            foreach (var mappedBoon in map)
            {
                var index = boonList.IndexOf(mappedBoon.First["id"].Value<string>());

                if (index != -1)
                {
                    maps.Add(new BuffModel
                    {
                        Icon = mappedBoon.First["icon"].Value<string>(),
                        Name = mappedBoon.First["name"].Value<string>(),
                        Stacking = mappedBoon.First["stacking"].Value<bool>(),
                        Id = mappedBoon.First["id"].Value<string>(),
                        Index = index,
                    });
                }
            }

            var phase = logData["phases"][0];
            // Newer EI versions moved boonStats into buffsStatContainer
            var buffs = phase["buffsStatContainer"]?["boonStats"] ?? phase["boonStats"];
            maps = maps.OrderBy(i => i.Index).ToList();

            for (var i = 0; i < maps.Count; i++)
            {
                var x = buffs[playerModel.Index]["data"][i];

                switch (x.Count())
                {
                    case 1:
                        maps[i].Value = x[0].Value<double>();
                        maps[i].Stacks = null;
                        break;

                    case 2:
                        maps[i].Value = x[0].Value<double>();
                        maps[i].Stacks = x[1].Value<double>();
                        break;

                    default:
                        maps[i].Value = 0;
                        maps[i].Stacks = null;
                        break;
                }
            }

            playerModel.Buffs.AddRange(maps);
        }

        private static void ParseMechanics(this PlayerModel playerModel, RaidModel raidModel, JObject logData,
            ILogger logger = null)
        {
            var strategy = MechanicsFactory.FindStrategy(raidModel.EncounterIcon);
            try
            {
                strategy?.Parse(logData, playerModel);
            }
            catch (Exception)
            {
                logger?.LogDebug(">>> Mechanics not parsable: " + raidModel.LogUrl);
            }
        }

        private static void ParseDamageStats(this PlayerModel playerModel, long fightDuration, JObject logData)
        {
            var targets = logData["players"][playerModel.Index]["details"]["dmgDistributionsTargets"][0];
            playerModel.Damage = 0;

            for (var i = 0; i < logData["phases"][0]["targets"].Count(); i++)
            {
                playerModel.Damage += targets[i]["totalDamage"].Value<long>();
            }

            playerModel.Dps = playerModel.Damage * 1000 / fightDuration;
        }

        private static void ParseSupportStats(this PlayerModel playerModel, JObject logData)
        {
            var supportStats = logData["phases"][0]["supportStats"][playerModel.Index];

            playerModel.ResAmount = supportStats[6].Value<int>();
            playerModel.ResTime = supportStats[7].Value<double>();
            playerModel.Cc = logData["players"][playerModel.Index]["details"]["dmgDistributions"][0]["totalBreakbarDamage"].Value<long>();
        }

        private static void ParseDefStats(this PlayerModel playerModel, JObject logData)
        {
            var defStats = logData["phases"][0]["defStats"][playerModel.Index];
            var length = defStats.Count();

            playerModel.TimesDowned = defStats[length - 4].Value<int>();
            playerModel.DownedText = defStats[length - 3].Value<string>();
            playerModel.TimesDied = defStats[length - 2].Value<int>();
            playerModel.DiedText = defStats[length - 1].Value<string>();
        }

        private static void SetGeneralInformation(this RaidModel raidModel, JObject logData)
        {
            raidModel.EncounterTime = logData["encounterDuration"].Value<string>();
            raidModel.EncounterIcon = logData["fightIcon"].Value<string>();
            raidModel.EncounterName = logData["fightName"].Value<string>();
            if (logData["fightID"] != null)
            {
                raidModel.EncounterId = logData["fightID"].Value<int>();
            }
            raidModel.Killed = logData["success"].Value<bool>();
        }

        private static void SetRemainingHealth(this RaidModel raidModel, JObject logData)
        {
            for (var i = 0; i < logData["phases"][0]["targets"].Count(); i++)
            {
                var targetData = logData["targets"][logData["phases"][0]["targets"][i].Value<int>()];
                raidModel.HpLeft.Add(targetData["hpLeft"].Value<double>());
            }
        }

        private static OneOf<JObject, NotFound> LoadLogData(string file)
        {
            var sr = new StreamReader(file);
            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine();
                if (line.Contains("var _logData"))
                {
                    var indexStart = line.IndexOf("var _logData = ", StringComparison.Ordinal);
                    var indexEnd = line.IndexOf("]};", indexStart, StringComparison.Ordinal) + 2;
                    var json = line[(indexStart + 15)..indexEnd];
                    sr.Close();
                    return JObject.Parse(json);
                }
            }
            sr.Close();

            return new NotFound();
        }
    }
}