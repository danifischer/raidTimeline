using Cocona;
using raidTimeline.App.Services.Interfaces;
using raidTimeline.Database.Services;

namespace raidTimeline.App.Commands;

internal static class DatabaseCommands
{
    internal static CoconaApp RegisterDatabaseCommands(this CoconaApp app)
    {
        app.AddCommand("db", (
            [FromService] IEncounterService encounterService,
            [FromService] IFileHandlingService fileHandlingService,
            [FromService] IParserService parserService
            ) =>
        {
            var now = DateTime.Now;
            var limit = DateTime.Now.AddMonths(-2);
            
            for (; limit < now;)
            {
                parserService.ParseLogsFromDisk(limit.ToString("yyyyMMdd"), false, false, false, 
                    new CancellationToken(), encounterService);
                fileHandlingService.DeleteHtmlAllFiles();
                limit = limit.AddDays(1);
            }
        });
        
        return app;
    }
}