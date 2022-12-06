using Microsoft.Extensions.DependencyInjection;
using raidTimeline.Database.Context;
using raidTimeline.Database.Interfaces;
using raidTimeline.Database.Services;

namespace raidTimeline.Database;

public static class DependencyInjection
{
    public static void RegisterDatabaseServices(this IServiceCollection services)
    {
        services.AddTransient<IEncounterDataService, EncounterDataService>();
        services.AddTransient<IGeneralStatisticsDataService, GeneralStatisticsDataService>();
        services.AddTransient<IEncounterService, EncounterService>();
        services.AddTransient<RaidContext>();
        services.AddTransient<Func<RaidContext>>(provider =>
        {
            return () => provider.GetService<RaidContext>()!;
        });
    }
}