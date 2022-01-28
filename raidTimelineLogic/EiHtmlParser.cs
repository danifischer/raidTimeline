using Newtonsoft.Json;
using raidTimelineLogic.Mechanics;
using raidTimelineLogic.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("raidTimelineLogicTests")]

namespace raidTimelineLogic
{
    internal class EiHtmlParser
    {
        private readonly MechanicsFactory _mechanicsFactory;

        public EiHtmlParser()
        {
            _mechanicsFactory = MechanicsFactory.GetMechanicsFactory();
        }

        internal RaidModel ParseLog(string filePath)
        {
            var model = new RaidModel(filePath);

            try
            {
                var logData = GetLogData(File.ReadAllText(model.LogPath));

#if DEBUG
                File.WriteAllText(@"d:\temp.json", JsonConvert.SerializeObject(logData));
#endif

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
                        AccountName = logData.players[j].acc
                    };

                    ParseSupportStats(logData, playerModel);
                    ParseDamageStats(logData, playerModel, fightDuration);
                    ParseMechanics(logData, playerModel, model);
                    ParseBoons(logData, playerModel);

                    model.Players.Add(playerModel);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($">>> {filePath} cannot be parsed.");
#if DEBUG
                Console.WriteLine(e.Message);
#endif
                return null;
            }

            return model;
        }

        private static void ParseBoons(dynamic logData, PlayerModel playerModel)
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

        private void ParseMechanics(dynamic logData, PlayerModel playerModel, RaidModel raidModel)
        {
            var strategy = _mechanicsFactory.FindStrategy(raidModel.EncounterIcon);
            try
            {
                strategy?.Parse(logData, playerModel);
            }
            catch (Exception)
            {
                Console.WriteLine(">>> Mechanics not parsable: " + raidModel.LogUrl);
            }
        }

        private static void ParseDamageStats(dynamic logData, PlayerModel playerModel, long fightDuration)
        {
            var targets = logData.players[playerModel.Index].details.dmgDistributionsTargets[0];
            playerModel.Damage = 0;

            for (var i = 0; i < logData.phases[0].targets.Count; i++)
            {
                playerModel.Damage += (long)targets[i].totalDamage.Value;
            }

            playerModel.Dps = playerModel.Damage * 1000 / fightDuration;
        }

        private static void ParseSupportStats(dynamic logData, PlayerModel playerModel)
        {
            playerModel.ResAmmount = (int)logData.phases[0].supportStats[playerModel.Index][6];
            playerModel.ResTime = (double)logData.phases[0].supportStats[playerModel.Index][7];
            playerModel.Cc = (long)logData.players[playerModel.Index].details.dmgDistributions[0]
                .totalBreakbarDamage.Value;
        }

        private static void SetGeneralInformation(RaidModel model, dynamic logData)
        {
            model.EncounterTime = logData.encounterDuration.Value;
            model.EncounterIcon = logData.fightIcon.Value;
            model.EncounterName = logData.fightName.Value;
            model.Killed = logData.success.Value;
        }

        private static void SetRemainingHealth(RaidModel model, dynamic logData)
        {
            for (var i = 0; i < logData.phases[0].targets.Count; i++)
            {
                var targetData = logData.targets[(int)logData.phases[0].targets[i]];
                model.HpLeft.Add(targetData.hpLeft.Value);
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