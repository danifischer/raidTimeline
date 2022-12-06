using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using raidTimeline.Logic.Mechanics;
using raidTimeline.Logic.Models;

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
                var logData = GetLogData(File.ReadAllText(model.LogPath));

                SetRemainingHealth(model, logData);
                SetGeneralInformation(model, logData);
                if (logData.eiVersion != null || logData.parser != null)
                {
                    model.OccurenceStart = DateTime.ParseExact(
                        logData.encounterStart.Value, "yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture);
                    model.OccurenceEnd = DateTime.ParseExact(
                        logData.encounterEnd.Value, "yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture);
                }

                var fightDuration = (long)logData.phases[0].duration.Value;

                for (var j = 0; j < logData.players.Count; j++)
                {
                    var playerModel = new PlayerModel
                    {
                        Index = j,
                        AccountName = logData.players[j].acc,
                        Profession = logData.players[j].profession,
                        ProfessionIcon = logData.players[j].icon,
                        IsNpc = logData.players[j].notInSquad,
                        Group = logData.players[j].group
                    };

                    ParseSupportStats(playerModel, logData);
                    ParseDamageStats(playerModel, fightDuration, logData);
                    ParseMechanics(playerModel, model, logData, logger);
                    ParseBoons(playerModel, logData);

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

        private static void ParseBoons(this PlayerModel playerModel, dynamic logData)
        {
            var boons = logData.boons;
            var boonList = new List<string>();

            foreach (var boon in boons)
            {
                boonList.Add(boon.Value.ToString());
            }

            var maps = new List<BuffModel>();
            var map = logData.buffMap;

            foreach (var mappedBoon in map)
            {
                var index = boonList.IndexOf(mappedBoon.First.id.Value.ToString());

                if (index != -1)
                {
                    maps.Add(new BuffModel
                    { 
                        Icon = mappedBoon.First.icon,
                        Name = mappedBoon.First.name,
                        Stacking = mappedBoon.First.stacking,
                        Id = mappedBoon.First.id,
                        Index = index,
                    });
                }
            }

            var buffs = logData.phases[0].boonStats;
            maps = maps.OrderBy(i => i.Index).ToList();

            for (var i = 0; i < maps.Count; i++)
            {
                var x = buffs[playerModel.Index].data[i];

                switch (x.Count)
                {
                    case 1:
                        maps[i].Value = x[0];
                        maps[i].Stacks = null;
                        break;
                    case 2:
                        maps[i].Value = x[0];
                        maps[i].Stacks = x[1];
                        break;
                    default:
                        maps[i].Value = 0;
                        maps[i].Stacks = null;
                        break;
                }
            }

            playerModel.Buffs.AddRange(maps);
        }

        private static void ParseMechanics(this PlayerModel playerModel, RaidModel raidModel, dynamic logData,
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

        private static void ParseDamageStats(this PlayerModel playerModel, long fightDuration, dynamic logData)
        {
            var targets = logData.players[playerModel.Index].details.dmgDistributionsTargets[0];
            playerModel.Damage = 0;

            for (var i = 0; i < logData.phases[0].targets.Count; i++)
            {
                playerModel.Damage += (long)targets[i].totalDamage.Value;
            }

            playerModel.Dps = playerModel.Damage * 1000 / fightDuration;
        }

        private static void ParseSupportStats(this PlayerModel playerModel, dynamic logData)
        {
            playerModel.ResAmount = (int)logData.phases[0].supportStats[playerModel.Index][6];
            playerModel.ResTime = (double)logData.phases[0].supportStats[playerModel.Index][7];
            playerModel.Cc = (long)logData.players[playerModel.Index].details.dmgDistributions[0]
                .totalBreakbarDamage.Value;
        }

        private static void SetGeneralInformation(this RaidModel raidModel, dynamic logData)
        {
            raidModel.EncounterTime = logData.encounterDuration.Value;
            raidModel.EncounterIcon = logData.fightIcon.Value;
            raidModel.EncounterName = logData.fightName.Value;
            raidModel.EncounterId = int.Parse(logData.fightID.Value.ToString());
            raidModel.Killed = logData.success.Value;
        }

        private static void SetRemainingHealth(this RaidModel raidModel, dynamic logData)
        {
            for (var i = 0; i < logData.phases[0].targets.Count; i++)
            {
                var targetData = logData.targets[(int)logData.phases[0].targets[i]];
                raidModel.HpLeft.Add(targetData.hpLeft.Value);
            }
        }

        private static dynamic GetLogData(string encounter)
        {
            var indexStart = encounter.IndexOf("var _logData = ", StringComparison.Ordinal);
            var indexEnd = encounter.IndexOf("]};", indexStart, StringComparison.Ordinal) + 2;
            var json = encounter.Substring(indexStart + 15, indexEnd - (indexStart + 15));
            return JsonConvert.DeserializeObject(json);
        }
    }
}