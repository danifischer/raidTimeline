using Cocona;
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
                        [Option('d',
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
                        uploadService.UploadFilesToDpsReport(day, killOnly, filter);
                    })
                    .WithDescription("Upload log files to dps.report.");
                
                command.AddCommand("api", (
                        [FromService] IUploadService uploadService,
                        [Option('d',
                            Description = "The day which should be uploaded (format: yyyyMMdd). Default is today.")]
                        string? day,
                        [Option('k',
                            Description = "Uploads only prints the html files that killed the boss.")]
                        bool killOnly,
                        [Option('r',
                            Description = "The raid group which the html files belong to.")]
                        string raidGroup
                    ) =>
                    {
                        uploadService.UploadFilesToEndpoint(day, raidGroup, killOnly);
                    })
                    .WithDescription("Uploads html files without the timeline file to a defined api endpoint.");
            })
            .WithDescription("Commands for uploading html and log files.");

        return app;
    }
}