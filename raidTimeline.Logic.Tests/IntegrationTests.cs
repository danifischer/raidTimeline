using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace raidTimeline.Logic.Tests;

/// <summary>
/// Runs a real Elite Insights command line parser on the test log and parses its output.
/// Opt-in: set the environment variable <c>EI_CLI_PATH</c> to <c>GuildWars2EliteInsights-CLI.exe</c>
/// (from GW2EICLI.zip of https://github.com/baaron4/GW2-Elite-Insights-Parser/releases) and run
/// <c>dotnet test --filter Category=Integration</c>. Catches changes in the Elite Insights json format.
/// </summary>
public class IntegrationTests
{
    private static readonly string TestLog = Path.Combine(AppContext.BaseDirectory, "Files", "test_log.zevtc");

    [Fact]
    [Trait("Category", "Integration")]
    public void ParseLog_RealEliteInsightsOutput_ShouldParse()
    {
        var cli = Environment.GetEnvironmentVariable("EI_CLI_PATH");
        if (string.IsNullOrEmpty(cli) || !File.Exists(cli))
        {
            // Not a failure: the test needs a local Elite Insights installation.
            return;
        }

        // Elite Insights writes its output next to the log, so work on a copy.
        using var temp = TestFiles.CreateTempDirectory();
        var log = Path.Combine(temp.Path, "test_log.zevtc");
        File.Copy(TestLog, log);
        var config = Path.Combine(temp.Path, "ei.conf");
        File.WriteAllLines(config, new[]
        {
            "SaveOutJSON=true", "SaveOutHTML=false", "SaveOutTrace=false", "UploadToDPSReports=false",
            "UploadToWingman=false"
        });

        var startInfo = new ProcessStartInfo(cli) { UseShellExecute = false, RedirectStandardOutput = true };
        startInfo.ArgumentList.Add("-c");
        startInfo.ArgumentList.Add(config);
        startInfo.ArgumentList.Add(log);
        using var process = Process.Start(startInfo)!;
        process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        var json = Directory.GetFiles(temp.Path, "*.json").Single();
        var model = EiJsonParser.ParseFile(json);

        model.Should().NotBeNull();
        model!.EncounterName.Should().Be("Vale Guardian");
        model.Players.Should().HaveCount(10);
        model.Players.Sum(p => p.Damage).Should().BeGreaterThan(0);
    }
}
