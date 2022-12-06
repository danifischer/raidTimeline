using raidTimeline.Database.Converters;
using raidTimeline.Database.Interfaces;
using raidTimeline.Database.Models;
using raidTimeline.Logic.Models;

namespace raidTimeline.Database.Services;

public class EncounterService : IEncounterService
{
    private readonly IEncounterDataService _encounterDataService;
    private readonly IGeneralStatisticsDataService _generalStatisticsDataService;

    public EncounterService(IEncounterDataService encounterDataService, 
        IGeneralStatisticsDataService generalStatisticsDataService)
    {
        _encounterDataService = encounterDataService;
        _generalStatisticsDataService = generalStatisticsDataService;
    }
    
    public async Task AddRaidModel(RaidModel raidModel)
    {
        var encounter = RaidModelToEncounterConverter.ConvertEncounter(raidModel);

        var boss = RaidModelToEncounterConverter.ConvertBoss(raidModel);
        encounter.Boss = await _encounterDataService.GetBoss(boss.FightId) ?? boss; 
        
        encounter.Players = new List<Player>();

        var statistics = new List<GeneralStatistics>();

        foreach (var playerModel in raidModel.Players)
        {
            var player = PlayerModelToPlayerConverter.ConvertPlayer(playerModel);
            player = await _encounterDataService.GetPlayer(player.AccountName) ?? player;

            var statistic = new GeneralStatistics
            {
                Encounter = encounter,
                Player = player,
                Cc = playerModel.Cc,
                Damage = playerModel.Damage,
                Dps = playerModel.Dps,
                ResAmount = playerModel.ResAmount,
                ResTime = playerModel.ResTime
            };

            statistics.Add(statistic);
            encounter.Players.Add(player);
        }
        
        await _encounterDataService.Insert(encounter);
        await _generalStatisticsDataService.Insert(statistics.ToArray());
    }
}