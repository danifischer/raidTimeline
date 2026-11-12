using Microsoft.Extensions.Configuration;
using raidTimeline.App.Helpers;
using Xunit;

namespace raidTimeline.App.Tests;

public sealed class ConfigurationTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "rta-app-tests-" + Guid.NewGuid().ToString("N"));
    private readonly AppPaths _paths;

    public ConfigurationTests()
    {
        _paths = new AppPaths(Path.Combine(_root, "program"), Path.Combine(_root, "user", "rta"),
            Path.Combine(_root, "logs"));
        Directory.CreateDirectory(_paths.BaseDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }

    private IConfiguration Load()
    {
        var builder = new ConfigurationBuilder();
        builder.AddAppConfigFiles(_paths);
        return builder.Build();
    }

    private void WriteDefault(string json) => File.WriteAllText(_paths.DefaultConfigFile, json);

    private void WriteUser(string json)
    {
        Directory.CreateDirectory(_paths.ConfigDirectory);
        File.WriteAllText(_paths.UserConfigFile, json);
    }

    [Fact]
    public void AppPaths_Default_UsesTheUserProfile()
    {
        var paths = AppPaths.Create("program", null);

        Assert.Equal("program", paths.BaseDirectory);
        Assert.Equal(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "rta"),
            paths.ConfigDirectory);
        Assert.Equal(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "rta"),
            paths.LogDirectory);
        Assert.EndsWith(Path.Combine("rta", "config.json"), paths.UserConfigFile);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AppPaths_EmptyHome_IsIgnored(string? home)
    {
        Assert.EndsWith("rta", AppPaths.Create("program", home).ConfigDirectory);
    }

    [Fact]
    public void AppPaths_WithHome_KeepsSettingsAndLogsThere()
    {
        var paths = AppPaths.Create("program", _root);

        Assert.Equal(Path.GetFullPath(_root), paths.ConfigDirectory);
        Assert.Equal(Path.GetFullPath(_root), paths.LogDirectory);
        Assert.Equal(Path.Combine(Path.GetFullPath(_root), "config.json"), paths.UserConfigFile);
    }

    [Fact]
    public void AddAppConfigFiles_OnlyTheDefaultsExist_UsesThem()
    {
        WriteDefault("""{ "eiPath": "default", "bossEncounterIdFilter": "0x3C4E" }""");

        var configuration = Load();

        Assert.Equal("default", configuration["eiPath"]);
        Assert.Equal("0x3C4E", configuration["bossEncounterIdFilter"]);
        Assert.False(Directory.Exists(_paths.ConfigDirectory), "reading must not create anything");
    }

    [Fact]
    public void AddAppConfigFiles_LaterFilesOverrideEarlierOnes()
    {
        WriteDefault("""{ "a": "default", "b": "default", "c": "default" }""");
        File.WriteAllText(_paths.LocalConfigFile, """{ "b": "local", "c": "local" }""");
        WriteUser("""{ "c": "user" }""");

        var configuration = Load();

        Assert.Equal("default", configuration["a"]);
        Assert.Equal("local", configuration["b"]);
        Assert.Equal("user", configuration["c"]);
    }

    [Fact]
    public void AddAppConfigFiles_UserFileMayHaveCommentsAndTrailingCommas()
    {
        WriteDefault("{ }");
        WriteUser("""
            {
              // my settings
              "eiPath": "C:\\ei\\ei.exe",
            }
            """);

        Assert.Equal(@"C:\ei\ei.exe", Load()["eiPath"]);
    }

    [Fact]
    public void AddAppConfigFiles_MissingDefaultFile_Throws()
    {
        Assert.ThrowsAny<FileNotFoundException>(() => Load());
    }

    [Fact]
    public void CreateUserConfigIfMissing_StartsWithTheCurrentSettings()
    {
        // someone who edited the config.json next to the program must not lose the values
        WriteDefault("""
            { "eiPath": "C:\\Tools\\ei.exe", "dpsReportToken": "abc\"def", "bossEncounterIdFilter": "0x3C4E" }
            """);

        var created = ConfigurationSources.CreateUserConfigIfMissing(_paths, Load());

        Assert.True(created);
        Assert.True(File.Exists(_paths.UserConfigFile));
        var reloaded = Load();
        Assert.Equal(@"C:\Tools\ei.exe", reloaded["eiPath"]);
        Assert.Equal("abc\"def", reloaded["dpsReportToken"]);
        Assert.Equal("", reloaded["logPath"]);
        // the default filter stays in the shipped config.json
        Assert.Equal("0x3C4E", reloaded["bossEncounterIdFilter"]);
        Assert.DoesNotContain("bossEncounterIdFilter\":", File.ReadAllText(_paths.UserConfigFile));
    }

    [Fact]
    public void CreateUserConfigIfMissing_ListsEverySettingAHumanSets()
    {
        WriteDefault("{ }");

        ConfigurationSources.CreateUserConfigIfMissing(_paths, Load());

        var configuration = Load();
        foreach (var key in ConfigurationSources.UserKeys)
        {
            Assert.Contains(key, configuration.GetChildren().Select(c => c.Key));
        }
    }

    [Fact]
    public void CreateUserConfigIfMissing_ExistingFile_IsNeverChanged()
    {
        WriteDefault("""{ "eiPath": "default" }""");
        WriteUser("""{ "eiPath": "mine" }""");

        var created = ConfigurationSources.CreateUserConfigIfMissing(_paths, Load());

        Assert.False(created);
        Assert.Equal("""{ "eiPath": "mine" }""", File.ReadAllText(_paths.UserConfigFile));
    }

    [Fact]
    public void CreateUserConfigIfMissing_CreatesTheFolder()
    {
        WriteDefault("{ }");
        Assert.False(Directory.Exists(_paths.ConfigDirectory));

        ConfigurationSources.CreateUserConfigIfMissing(_paths, Load());

        Assert.True(Directory.Exists(_paths.ConfigDirectory));
    }
}
