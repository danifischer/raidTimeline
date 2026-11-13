using Cocona;
using CliWrap;
using raidTimeline.App.Services;

namespace raidTimeline.App.Commands;

internal static class UpdateCommands
{
    internal static CoconaApp RegisterUpdateCommands(this CoconaApp app)
    {
        app.AddCommand("update", async (
                [Option('c', Description = "Only checks if there is a newer release, installs nothing.")]
                bool check,
                CoconaAppContext context) =>
            {
                using var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
                return await RunAsync(new UpdateService(httpClient), check, AppContext.BaseDirectory,
                    context.CancellationToken);
            })
            .WithDescription("Installs the latest release from GitHub if it is newer than this version.");

        return app;
    }

    private static async Task<int> RunAsync(UpdateService service, bool checkOnly, string baseDirectory,
        CancellationToken cancellationToken)
    {
        UpdateService.DeleteOldFiles(baseDirectory);

        var current = UpdateService.CurrentVersion;
        ReleaseInfo release;
        try
        {
            release = await service.GetLatestReleaseAsync(cancellationToken);
        }
        catch (UpdateException e)
        {
            Console.Error.WriteLine(e.Message);
            return 1;
        }

        if (release.Version.CompareTo(current) <= 0)
        {
            Console.WriteLine($"rta {current} is up to date (latest release: {release.Version}).");
            return 0;
        }

        Console.WriteLine($"New release {release.Version} available (you have {current}).");
        if (checkOnly)
        {
            Console.WriteLine("Run \"rta update\" to install it.");
            return 0;
        }

        var folder = Path.Combine(Path.GetTempPath(), "rta-update-" + Guid.NewGuid().ToString("N"));
        try
        {
            if (UpdateService.IsDotnetTool(baseDirectory))
            {
                return await UpdateToolAsync(service, release, folder, cancellationToken);
            }

            if (!OperatingSystem.IsWindows())
            {
                Console.Error.WriteLine(
                    $"The single file release is only built for Windows. Download {release.Tag} from https://github.com/{UpdateService.Repository}/releases/latest");
                return 1;
            }

            var zip = await service.DownloadVerifiedAsync(release, UpdateService.ZipName(release.Version), folder,
                cancellationToken);
            UpdateService.InstallZip(zip, baseDirectory);
            Console.WriteLine($"Updated to {release.Version}. Your settings were not touched.");
            return 0;
        }
        catch (UpdateException e)
        {
            Console.Error.WriteLine(e.Message);
            return 1;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            Console.Error.WriteLine($"The update failed: {e.Message}");
            return 1;
        }
        finally
        {
            try
            {
                if (Directory.Exists(folder)) Directory.Delete(folder, true);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
            }
        }
    }

    private static async Task<int> UpdateToolAsync(UpdateService service, ReleaseInfo release, string folder,
        CancellationToken cancellationToken)
    {
        await service.DownloadVerifiedAsync(release, UpdateService.NupkgName(release.Version), folder, cancellationToken);

        var arguments = new[]
        {
            "tool", "update", "-g", "rta", "--add-source", folder, "--version", release.Version.ToString()
        };
        Console.WriteLine($"dotnet {string.Join(' ', arguments)}");

        var result = await Cli.Wrap("dotnet")
            .WithArguments(arguments)
            .WithStandardOutputPipe(PipeTarget.ToStream(Console.OpenStandardOutput()))
            .WithStandardErrorPipe(PipeTarget.ToStream(Console.OpenStandardError()))
            .WithValidation(CommandResultValidation.None)
            .ExecuteAsync(cancellationToken);

        if (result.ExitCode != 0)
        {
            Console.Error.WriteLine(
                $"dotnet tool update failed ({result.ExitCode}). Run \"dotnet tool update -g rta\" yourself.");
        }

        return result.ExitCode;
    }
}
