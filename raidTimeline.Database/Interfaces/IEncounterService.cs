using raidTimeline.Logic.Models;

namespace raidTimeline.Database.Services;

public interface IEncounterService
{
    Task AddRaidModel(RaidModel raidModel);
}