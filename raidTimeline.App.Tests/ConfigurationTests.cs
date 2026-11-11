using FluentAssertions;
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

        paths.BaseDirectory.Should().Be("program");
        paths.ConfigDirectory.Should().Be(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "rta"));
        paths.LogDirectory.Should().Be(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "rta"));
        paths.UserConfigFile.Should().EndWith(Path.Combine("rta", "config.json"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AppPaths_EmptyHome_IsIgnored(string? home)
    {
        AppPaths.Create("program", home).ConfigDirectory.Should().EndWith("rta");
    }

    [Fact]
    public void AppPaths_WithHome_KeepsSettingsAndLogsThere()
    {
        var paths = AppPaths.Create("program", _root);

        paths.ConfigDirectory.Should().Be(Path.GetFullPath(_root));
        paths.LogDirectory.Should().Be(Path.GetFullPath(_root));
        paths.UserConfigFile.Should().Be(Path.Combine(Path.GetFullPath(_root), "config.json"));
    }

    [Fact]
    public void AddAppConfigFiles_OnlyTheDefaultsExist_UsesThem()
    {
        WriteDefault("""{ "eiPath": "default", "bossEncounterIdFilter": "0x3C4E" }""");

        var configuration = Load();

        configuration["eiPath"].Should().Be("default");
        configuration["bossEncounterIdFilter"].Should().Be("0x3C4E");
        Directory.Exists(_paths.ConfigDirectory).Should().BeFalse("reading must not create anything");
    }

    [Fact]
    public void AddAppConfigFiles_LaterFilesOverrideEarlierOnes()
    {
        WriteDefault("""{ "a": "default", "b": "default", "c": "default" }""");
        File.WriteAllText(_paths.LocalConfigFile, """{ "b": "local", "c": "local" }""");
        WriteUser("""{ "c": "user" }""");

        var configuration = Load();

        configuration["a"].Should().Be("default");
        configuration["b"].Should().Be("local");
        configuration["c"].Should().Be("user");
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

        Load()["eiPath"].Should().Be(@"C:\ei\ei.exe");
    }

    [Fact]
    public void AddAppConfigFiles_MissingDefaultFile_Throws()
    {
        var act = () => Load();

        act.Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public void CreateUserConfigIfMissing_StartsWithTheCurrentSettings()
    {
        // someone who edited the config.json next to the program must not lose the values
        WriteDefault("""
            { "eiPath": "C:\\Tools\\ei.exe", "dpsReportToken": "abc\"def", "bossEncounterIdFilter": "0x3C4E" }
            """);

        var created = ConfigurationSources.CreateUserConfigIfMissing(_paths, Load());

        created.Should().BeTrue();
        File.Exists(_paths.UserConfigFile).Should().BeTrue();
        var reloaded = Load();
        reloaded["eiPath"].Should().Be(@"C:\Tools\ei.exe");
        reloaded["dpsReportToken"].Should().Be("abc\"def");
        reloaded["logPath"].Should().BeEmpty();
        reloaded["bossEncounterIdFilter"].Should().Be("0x3C4E", "the default filter stays in the shipped config.json");
        File.ReadAllText(_paths.UserConfigFile).Should().NotContain("bossEncounterIdFilter\":");
    }

    [Fact]
    public void CreateUserConfigIfMissing_ListsEverySettingAHumanSets()
    {
        WriteDefault("{ }");

        ConfigurationSources.CreateUserConfigIfMissing(_paths, Load());

        var configuration = Load();
        foreach (var key in ConfigurationSources.UserKeys)
        {
            configuration.GetChildren().Select(c => c.Key).Should().Contain(key);
        }
    }

    [Fact]
    public void CreateUserConfigIfMissing_ExistingFile_IsNeverChanged()
    {
        WriteDefault("""{ "eiPath": "default" }""");
        WriteUser("""{ "eiPath": "mine" }""");

        var created = ConfigurationSources.CreateUserConfigIfMissing(_paths, Load());

        created.Should().BeFalse();
        File.ReadAllText(_paths.UserConfigFile).Should().Be("""{ "eiPath": "mine" }""");
    }

    [Fact]
    public void CreateUserConfigIfMissing_CreatesTheFolder()
    {
        WriteDefault("{ }");
        Directory.Exists(_paths.ConfigDirectory).Should().BeFalse();

        ConfigurationSources.CreateUserConfigIfMissing(_paths, Load());

        Directory.Exists(_paths.ConfigDirectory).Should().BeTrue();
    }
}
