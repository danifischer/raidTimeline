using Microsoft.EntityFrameworkCore;
using raidTimeline.Database.Context;
using raidTimeline.Database.Interfaces;
using raidTimeline.Database.Models;

namespace raidTimeline.Database.Services;

internal class EncounterDataService : IEncounterDataService
{
    private readonly Func<RaidContext> _createContext;

    public EncounterDataService(Func<RaidContext> createContext)
    {
        _createContext = createContext;
    }

    public async Task Insert(Encounter encounter)
    {
        await using var context = _createContext();

        var dbBoss = await context.Bosses
            .FindAsync(encounter.Boss.Id);
        if (dbBoss != null)
        {
            encounter.Boss = dbBoss;
        }

        var playerList = new List<Player>();
        foreach (var player in encounter.Players)
        {
            var dbPlayer = await context.Players
                .FindAsync(player.Id);

            playerList.Add(dbPlayer ?? player);
        }
        encounter.Players = playerList;

        await context.Encounters.AddAsync(encounter);
        await context.SaveChangesAsync();
    }

    public async Task<Player?> GetPlayer(string accountName)
    {
        await using var context = _createContext();
        var player = context.Players
            .SingleOrDefault(i => i.AccountName == accountName);
        return player;
    }
    
    public async Task<Boss?> GetBoss(int fightId)
    {
        await using var context = _createContext();
        var boss = context.Bosses
            .SingleOrDefault(i => i.FightId == fightId);
        return boss;
    }
}