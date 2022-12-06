using raidTimeline.Database.Models;
using raidTimeline.Logic.Models;

namespace raidTimeline.Database.Converters;

internal static class RaidModelToEncounterConverter
{
    internal static Encounter ConvertEncounter(RaidModel raidModel)
    {
        var encounter = new Encounter
        {
            Killed = raidModel.Killed,
            EncounterTime = raidModel.EncounterTime,
            OccurenceStart = raidModel.OccurenceStart.ToUniversalTime(),
            OccurenceEnd = raidModel.OccurenceEnd.ToUniversalTime(),
            HitPointsRemaining = raidModel.HpLeft.Average()
        };
        
        return encounter;
    }

    internal static Boss ConvertBoss(RaidModel raidModel)
    {
        return new Boss
        {
            FightId = raidModel.EncounterId,
            Icon = raidModel.EncounterIcon
        };
    }
}