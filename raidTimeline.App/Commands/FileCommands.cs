using Cocona;
using raidTimeline.App.Services.Interfaces;

namespace raidTimeline.App.Commands;

internal static class FileCommands
{
    internal static CoconaApp RegisterFileCommands(this CoconaApp app)
    {
        app.AddSubCommand("clean", command =>
            {
                command.AddCommand("html", (
                        [FromService] IFileHandlingService fileHandlingService) =>
                    {
                        fileHandlingService.DeleteHtmlAllFiles();
                    })
                    .WithDescription("Deletes all html log files from the output folder.");
                command.AddCommand("logs",
                    ([FromService] IFileHandlingService fileHandlingService,
                        [Option('d',
                            Description = "...")]
                        int? days,
                        [Option('a', Description = "Select all logs.")]
                        bool all,
                        [Option('f', Description = "Force the delete.")]
                        bool force
                    ) =>
                    {
                        fileHandlingService.DeleteLogFiles(days, all, force);
                    })
                    .WithDescription("Deletes arcdps log files from the log folder.");
            })
            .WithDescription("Commands to clean log file folders and free up space.");

        app.AddSubCommand("size", command =>
            {
                command.AddCommand("html", (
                        [FromService] IFileHandlingService fileHandlingService) =>
                    {
                        fileHandlingService.PrintHtmlFileSize();
                    })
                    .WithDescription("Show how much disk space is taken by html log files.");
                command.AddCommand("logs", (
                        [FromService] IFileHandlingService fileHandlingService) =>
                    {
                        fileHandlingService.PrintLogFileSize();
                    })
                    .WithDescription("Show how much disk space is taken by arcdps log files.");
            })
            .WithDescription("Commands to show the disk space taken by logs.");

        app.AddCommand("open", (
                [FromService] IFileHandlingService fileHandlingService) =>
            {
                fileHandlingService.OpenTimeline();
            })
            .WithDescription("Opens the current timeline file.");

        return app;
    }
}