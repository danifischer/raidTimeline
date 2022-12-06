using System.Collections.Generic;
using System.Linq;

namespace raidTimeline.Logic.Models
{
    internal record TableCell(string AccountName, string Profession, string ProfessionIcon);

    internal class TableModel
    {
        public TableModel(RaidModel raid, string[] players)
        {
            Cells = new List<TableCell>();

            foreach (var player in players)
            {
                var playerInRaid = raid.Players.SingleOrDefault(i => i.AccountName == player);
                if (playerInRaid != null)
                {
                    Cells.Add(new TableCell(playerInRaid.AccountName, playerInRaid.Profession, playerInRaid.ProfessionIcon));
                }
                else
                {
                    Cells.Add(new TableCell(player, "-", string.Empty));
                }
            }

            Encounter = raid.EncounterName;
            Killed = raid.Killed;
        }

        public List<TableCell> Cells { get; }
        public string Encounter { get; }
        public bool Killed { get; }
    }
}