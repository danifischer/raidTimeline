using raidTimeline.Models.Models;

namespace raidTimeline.Database.Interfaces;

public interface IEncounterService
{
    Task AddRaidModel(RaidModel raidModel);
}