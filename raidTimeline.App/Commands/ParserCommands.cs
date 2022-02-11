using Cocona;
using raidTimeline.App.Services.Interfaces;

namespace raidTimeline.App.Commands;

internal static class ParserCommands
{
    internal static CoconaApp RegisterParserCommands(this CoconaApp app)
    {
        app.AddSubCommand("parse", command =>
            {
                command.AddCommand("local",
                        ([FromService] IParserService parserService,
                            [Option('d',
                                Description = "The day which should be parsed (format: yyyyMMdd). Default is today.")]
                            string? day,
                            [Option('r', Description = "Creates the timeline in reverse date order.")]
                            bool reverse,
                            [Option('k', Description = "Only uses the logs that killed the boss for the timeline.")]
                            bool killOnly,
                            [Option('f',
                                Description =
                                    "Only uses the logs that are in the list of boss ids (in the config.json file) for the timeline.")]
                            bool filter
                        ) =>
                        {
                            parserService.ParseLogsFromDisk(day, reverse, killOnly, filter);
                        })
                    .WithDescription("Parses local log files with Elite Insights.");
                
                command.AddCommand("live",
                        ([FromService] IParserService parserService,
                            CoconaAppContext context, 
                            [Option('r', Description = "Creates the timeline in reverse date order.")]
                            bool reverse,
                            [Option('k', Description = "Only uses the logs that killed the boss for the timeline.")]
                            bool killOnly,
                            [Option('f',
                                Description =
                                    "Only uses the logs that are in the list of boss ids (in the config.json file) for the timeline.")]
                            bool filter
                        ) =>
                        {
                            parserService.ParseLogsFromDiskLive(reverse, killOnly, filter, context);
                        })
                    .WithDescription("Parses local log files with Elite Insights and keeps watching for new logs.");

                command.AddCommand("report",
                        ([FromService] IParserService parserService,
                            [Option('d',
                                Description = "The day which should be parsed (format: yyyyMMdd). Default is today.")]
                            string? day,
                            [Option('r', Description = "Creates the timeline in reverse date order.")]
                            bool reverse,
                            [Option('k', Description = "Only parses the logs that killed the boss.")]
                            bool killOnly,
                            [Option('f',
                                Description =
                                    "Only uses the logs that are in the list of boss ids (in the config.json file) for the timeline.")]
                            bool filter
                        ) =>
                        {
                            parserService.ParseLogsFromDpsReport(day, reverse, killOnly, filter);
                        })
                    .WithDescription("Parses remote log files from dps.report.");
            })
            .WithDescription("Commands for parsing log files.");

        return app;
    }
}