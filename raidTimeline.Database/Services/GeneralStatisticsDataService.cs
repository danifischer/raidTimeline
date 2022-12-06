using raidTimeline.Database.Context;
using raidTimeline.Database.Interfaces;
using raidTimeline.Database.Models;

namespace raidTimeline.Database.Services;

public class GeneralStatisticsDataService : IGeneralStatisticsDataService
{
    private readonly Func<RaidContext> _createContext;

    public GeneralStatisticsDataService(Func<RaidContext> createContext)
    {
        _createContext = createContext;
    }

    public async Task Insert(GeneralStatistics[] generalStatistics)
    {
        await using var context = _createContext();
        
        foreach (var generalStatistic in generalStatistics)
        {
            generalStatistic.Player = (await context.Players
                .FindAsync(generalStatistic.Player.Id))!;
            generalStatistic.Encounter = (await context.Encounters
                .FindAsync(generalStatistic.Encounter.Id))!;
        }
        
        await context.GeneralStatistics.AddRangeAsync(generalStatistics);
        await context.SaveChangesAsync();
    }
}