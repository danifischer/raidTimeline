using System.Collections.Generic;
using System.Linq;

namespace raidTimeline.Logic.Models
{
    internal record EncounterTableCell(string AccountName, string Profession, string ProfessionIcon);

    internal class EncounterTableModel
    {
        public EncounterTableModel(RaidModel raid, string[] players)
        {
            Cells = new List<EncounterTableCell>();

            foreach (var player in players)
            {
                var playerInRaid = raid.Players.SingleOrDefault(i => i.AccountName == player);
                if (playerInRaid != null)
                {
                    Cells.Add(new EncounterTableCell(playerInRaid.AccountName, playerInRaid.Profession, playerInRaid.ProfessionIcon));
                }
                else
                {
                    Cells.Add(new EncounterTableCell(player, "-", string.Empty));
                }
            }

            Encounter = raid.EncounterName;
            Url = raid.LogUrl;
            Killed = raid.Killed;
        }

        public List<EncounterTableCell> Cells { get; }
        public string Encounter { get; }
        public string Url { get; }
        public bool Killed { get; }
    }
}