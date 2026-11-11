using Microsoft.Extensions.Logging.Abstractions;
using raidTimeline.App.Services;
using Xunit;

namespace raidTimeline.App.Tests;

/// <summary>
/// Uses a small script as a stand-in for Elite Insights (called like it: <c>-c config log</c>), which writes
/// <c>&lt;log&gt;.done</c> after a short delay.
/// </summary>
public sealed class EliteInsightsServiceTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "rta-ei-tests-" + Guid.NewGuid().ToString("N"));
    private readonly string _log;
    private readonly EliteInsightsService _service = new(NullLogger<EliteInsightsService>.Instance);

    public EliteInsightsServiceTests()
    {
        Directory.CreateDirectory(_folder);
        _log = Path.Combine(_folder, "20261109-201500.zevtc");
        File.WriteAllBytes(_log, new byte[] { 1 });
    }

    public void Dispose()
    {
        if (Directory.Exists(_folder)) Directory.Delete(_folder, true);
    }

    private string CreateFakeEliteInsights(int exitCode)
    {
        if (OperatingSystem.IsWindows())
        {
            var cmd = Path.Combine(_folder, "ei.cmd");
            File.WriteAllText(cmd,
                $"@echo off\r\nping -n 3 127.0.0.1 >nul\r\necho done> \"%~3.done\"\r\nexit /b {exitCode}\r\n");
            return cmd;
        }

        var sh = Path.Combine(_folder, "ei.sh");
        File.WriteAllText(sh, $"#!/bin/sh\nsleep 1\necho done > \"$3.done\"\nexit {exitCode}\n");
        File.SetUnixFileMode(sh, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        return sh;
    }

    [Fact]
    public void ParseEiFile_WaitsUntilEliteInsightsIsFinished()
    {
        var ei = CreateFakeEliteInsights(0);

        var success = _service.ParseEiFile(_log, Path.Combine(_folder, "ei.conf"), ei);

        Assert.True(success);
        Assert.True(File.Exists(_log + ".done"), "the output has to exist when the method returns");
    }

    [Fact]
    public void ParseEiFile_EliteInsightsFails_ReturnsFalse()
    {
        var ei = CreateFakeEliteInsights(1);

        var success = _service.ParseEiFile(_log, Path.Combine(_folder, "ei.conf"), ei);

        Assert.False(success);
    }

    [Fact]
    public void ParseEiFile_ProgramDoesNotExist_ReturnsFalse()
    {
        var success = _service.ParseEiFile(_log, Path.Combine(_folder, "ei.conf"), Path.Combine(_folder, "missing.exe"));

        Assert.False(success);
    }
}
