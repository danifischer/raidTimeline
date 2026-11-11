using System.Diagnostics;
using Cocona;
using Microsoft.Extensions.Configuration;
using raidTimeline.App.Helpers;

namespace raidTimeline.App.Commands;

internal static class ConfigCommands
{
    internal static CoconaApp RegisterConfigCommands(this CoconaApp app)
    {
        app.AddSubCommand("config", command =>
            {
                command.AddCommand("edit", (
                        [FromService] AppPaths paths,
                        [FromService] IConfiguration configuration) =>
                    {
                        if (ConfigurationSources.CreateUserConfigIfMissing(paths, configuration))
                        {
                            Console.WriteLine($"Created {paths.UserConfigFile}");
                        }

                        Console.WriteLine(paths.UserConfigFile);
                        Open(paths.UserConfigFile);
                    })
                    .WithDescription("Opens your settings file (and creates it with the current settings if it does not exist).");

                command.AddCommand("path", (
                        [FromService] AppPaths paths) =>
                    {
                        Console.WriteLine(paths.UserConfigFile);
                    })
                    .WithDescription("Prints the path of your settings file.");

                command.AddCommand("log", (
                        [FromService] AppPaths paths) =>
                    {
                        Console.WriteLine(paths.LogDirectory);
                    })
                    .WithDescription("Prints the folder of the log files.");
            })
            .WithDescription("Commands for your settings.");

        return app;
    }

    private static void Open(string file)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = file, UseShellExecute = true });
        }
        catch (Exception e) when (e is System.ComponentModel.Win32Exception or InvalidOperationException
                                      or PlatformNotSupportedException)
        {
            Console.WriteLine($"Could not open the file ({e.Message}). Open it with an editor of your choice.");
        }
    }
}
