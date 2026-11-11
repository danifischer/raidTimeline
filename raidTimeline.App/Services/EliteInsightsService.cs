using System.Diagnostics;
using CliWrap;
using Microsoft.Extensions.Logging;
using raidTimeline.App.Services.Interfaces;

namespace raidTimeline.App.Services;

internal record EliteInsightsOptions
{
    internal EliteInsightsOptions(string outputPath, string logPath, bool saveOutJson = false,
        bool saveOutHtml = true)
    {
        OutputPath = outputPath;
        LogPath = logPath;
        SaveOutJson = saveOutJson;
        SaveOutHtml = saveOutHtml;
    }

    internal string OutputPath { get; }
    internal string LogPath { get; }

    /// <summary>Elite Insights writes the json files raidTimeline parses (<c>SaveOutJSON=true</c>).</summary>
    internal bool SaveOutJson { get; }

    /// <summary>Elite Insights writes the html logs the timeline links to (<c>SaveOutHTML=true</c>, the default).</summary>
    internal bool SaveOutHtml { get; }
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

            return new EliteInsightsOptions(outPath, logPath,
                ReadBooleanOption(text, "SaveOutJSON=", false),
                ReadBooleanOption(text, "SaveOutHTML=", true));
        }
        catch (Exception)
        {
            _logger.LogError("Non-valid configuration file.");
        }
        
        return null;
    }

    private static bool ReadBooleanOption(string configuration, string option, bool defaultValue)
    {
        var line = configuration.Split('\n')
            .Select(l => l.Trim())
            .FirstOrDefault(l => l.StartsWith(option, StringComparison.Ordinal));

        return line != null && bool.TryParse(line[option.Length..], out var value) ? value : defaultValue;
    }

    public async void ParseEiFile(string logFilePath, string configurationFilePath,
        string eliteInsightsExePath)
    {
        await Cli.Wrap(eliteInsightsExePath)
            .WithArguments(args => args
                .Add("-c")
                .Add($"{configurationFilePath}")
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
