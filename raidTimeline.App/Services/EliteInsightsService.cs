using System.Diagnostics;
using CliWrap;
using Microsoft.Extensions.Logging;
using raidTimeline.App.Services.Interfaces;

namespace raidTimeline.App.Services;

internal record EliteInsightsOptions
{
    internal EliteInsightsOptions(string outputPath, string logPath)
    {
        OutputPath = outputPath;
        LogPath = logPath;
    }

    internal string OutputPath { get; }
    internal string LogPath { get; }
}

internal class EliteInsightsService : IEliteInsightsService
{
    private readonly ILogger _logger;

    public EliteInsightsService(ILogger<EliteInsightsService> logger)
    {
        _logger = logger;
    }

    public EliteInsightsOptions? ReadConfigurationFile(string configurationPath)
    {
        try
        {
            var text = File.ReadAllText(configurationPath);
            const string eiPathOption = "AutoAddPath=";
            const string outPathOption = "OutLocation=";
            
            var eiPathOptionIndex = text.IndexOf(eiPathOption, StringComparison.Ordinal);
            var outPathOptionIndex = text.IndexOf(outPathOption, StringComparison.Ordinal);

            var logPath = text[(eiPathOptionIndex + eiPathOption.Length)..text.IndexOf("\n",
                eiPathOptionIndex, StringComparison.Ordinal)];
            var outPath = text[(outPathOptionIndex + outPathOption.Length)..text.IndexOf("\n",
                outPathOptionIndex, StringComparison.Ordinal)];

            return new EliteInsightsOptions(outPath, logPath);
        }
        catch (Exception)
        {
            _logger.LogError("Non-valid configuration file.");
        }
        
        return null;
    }

    public async void ParseEiFile(string logFilePath, string configurationFilePath,
        string eliteInsightsExePath)
    {
        await Cli.Wrap(eliteInsightsExePath)
            .WithArguments(args => args
                .Add($"-c {configurationFilePath}")
                .Add($"{logFilePath}")
                )
            .WithStandardErrorPipe(PipeTarget.ToDelegate(Console.WriteLine))
            .ExecuteAsync();
        /*
        var psi = new ProcessStartInfo
        {
            FileName = eliteInsightsExePath,
            Arguments = $"-c \"{configurationFilePath}\" \"{logFilePath}\"",
            UseShellExecute = false,
            RedirectStandardError = true
        };
        Process.Start(psi)?.WaitForExit();
        */
    }
}