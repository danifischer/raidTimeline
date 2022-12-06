using raidTimeline.Database.Models;

namespace raidTimeline.Database.Interfaces;

public interface IEncounterDataService
{
    Task Insert(Encounter encounter);
    Task<Player?> GetPlayer(string accountName);
    Task<Boss?> GetBoss(int fightId);
}