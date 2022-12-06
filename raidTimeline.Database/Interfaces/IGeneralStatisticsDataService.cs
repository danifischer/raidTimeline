using raidTimeline.Database.Models;

namespace raidTimeline.Database.Interfaces;

public interface IGeneralStatisticsDataService
{
    Task Insert(GeneralStatistics[] generalStatistics);
}