using raidTimeline.Database.Converters;
using raidTimeline.Database.Models;
using raidTimeline.Models.Models;

namespace raidTimeline.Database;

public static class ParsingTest
{
    public static bool Parse(RaidModel raidModel)
    {
        var encounter = RaidModelToEncounterConverter.ConvertEncounter(raidModel);
        encounter.Boss = RaidModelToEncounterConverter.ConvertBoss(raidModel);
        encounter.Players = new List<Player>();

        var statistics = new List<GeneralStatistics>();

        foreach (var playerModel in raidModel.Players)
        {
            var player = PlayerModelToPlayerConverter.ConvertPlayer(playerModel);
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

        return true;
    }
}