using raidTimeline.Database.DataModels;
using raidTimeline.Logic.Models;

namespace raidTimeline.Database.Converters;

internal static class RaidModelToEncounterConverter
{
    internal static Encounter Convert(RaidModel raidModel)
    {
        var encounter = new Encounter
        {
            Killed = raidModel.Killed,
            EncounterTime = raidModel.EncounterTime,
            OccurenceStart = raidModel.OccurenceStart,
            OccurenceEnd = raidModel.OccurenceEnd,
            HitPointsRemaining = raidModel.HpLeft.Average(),
            Boss = ConvertBossData(raidModel),
            Players = raidModel.Players
                .Select(PlayerModelToPlayerConverter.Convert).ToList()
        };
        
        return encounter;
    }

    private static Boss ConvertBossData(RaidModel raidModel)
    {
        return new Boss
        {
            FightId = raidModel.EncounterId,
            Icon = raidModel.EncounterIcon
        };
    }
}