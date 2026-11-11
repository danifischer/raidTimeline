using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace raidTimeline.Models.Models
{
    public record GeneralStatsTableModelCell(string AccountName, string TimesDownedString, int TimesDowned, string TimesDiedString, int TimesDied);

    public class GeneralStatsTableModel
    {
        public GeneralStatsTableModel(RaidModel raid, string[] players)
        {
            Cells = new List<GeneralStatsTableModelCell>();


            foreach (var player in players)
            {
                var playerInRaid = raid.Players.FirstOrDefault(i => i.AccountName == player);
                if (playerInRaid != null)
                {
                    Cells.Add(new GeneralStatsTableModelCell(playerInRaid.AccountName, 
                        playerInRaid.TimesDowned.ToString(), playerInRaid.TimesDowned, 
                        playerInRaid.TimesDied.ToString(), playerInRaid.TimesDied));
                }
                else
                {
                    Cells.Add(new GeneralStatsTableModelCell(player, "-", 0, "-", 0));
                }
            }


            Encounter = raid.EncounterName;
            Url = raid.LogUrl;
            Killed = raid.Killed;
        }

        public List<GeneralStatsTableModelCell> Cells { get; }
        public string Encounter { get; }
        public string Url { get; }
        public bool Killed { get; }
    }
}
