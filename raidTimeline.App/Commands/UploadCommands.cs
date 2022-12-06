using Cocona;
using raidTimeline.App.Helpers;
using raidTimeline.App.Services.Interfaces;

namespace raidTimeline.App.Commands;

public static class UploadCommands
{
    internal static CoconaApp RegisterUploadCommands(this CoconaApp app)
    {
        app.AddSubCommand("upload", command =>
            {
                command.AddCommand("report", (
                        [FromService] IUploadService uploadService,
                        CoconaAppContext context, 
                        [Day][Option('d',
                            Description = "The day which should be uploaded (format: yyyyMMdd). Default is today.")]
                        string? day,
                        [Option('k',
                            Description = "Uploads all log, but only prints the links to logs that killed the boss.")]
                        bool killOnly,
                        [Option('f',
                            Description =
                                "Only uses the logs that are in the list of boss ids (in the config.json file) for the printing links.")]
                        bool filter
                    ) =>
                    {
                        uploadService.UploadFilesToDpsReport(day, killOnly, filter, 
                            context.CancellationToken);
                    })
                    .WithDescription("Upload log files to dps.report.");
                
                command.AddCommand("api", (
                        [FromService] IUploadService uploadService,
                        CoconaAppContext context, 
                        [Day][Option('d',
                            Description = "The day which should be uploaded (format: yyyyMMdd). Default is today.")]
                        string? day,
                        [Option('k',
                            Description = "Uploads only prints the html files that killed the boss.")]
                        bool killOnly,
                        [RaidGroup][Option('r',
                            Description = "The raid group which the html files belong to.")]
                        string raidGroup,
                        [Option('f',
                            Description =
                                "Only uses the logs that are in the list of boss ids (in the config.json file) for the printing links.")]
                        bool filter
                    ) =>
                    {
                        uploadService.UploadFilesToEndpoint(day, raidGroup, killOnly, filter, 
                            context.CancellationToken);
                    })
                    .WithDescription("Uploads html files without the timeline file to a defined api endpoint.");
            })
            .WithDescription("Commands for uploading html and log files.");

        return app;
    }
}