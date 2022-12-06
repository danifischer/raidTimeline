using System;
using System.Collections.Generic;
using System.Linq;

namespace raidTimeline.Logic.Models
{
    internal class OverviewTableModel
    {
        public OverviewTableModel(RaidModel[] raids, TimeSpan totalTime)
        {
            Cells = new List<PlayerModel>();

            var lastTry = raids.OrderBy(i => i.OccurenceStart).Last();
            var tryTime = new TimeSpan(raids.Select(i => i.OccurenceEnd - i.OccurenceStart)
                .Sum(i => i.Ticks));

            BossName = lastTry.EncounterName;
            TimePercentage = (tryTime / totalTime).ToString("P1");
            Tries = $"{raids.Count(i => !i.Killed)}/{raids.Length}";
            Cells = lastTry.Players.Where(i => !i.IsNpc)
                .OrderBy(j => j.Group).ThenBy(k => k.AccountName).ToList();
            Url = lastTry.LogUrl;
        }

        public List<PlayerModel> Cells { get; }
        public string BossName { get; }
        public string TimePercentage { get; }
        public string Tries { get; }
        public string Url { get; }
    }
}