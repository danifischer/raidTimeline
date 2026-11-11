using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Xunit;

namespace raidTimeline.Logic.Tests;

/// <summary>
/// Runs the boss definitions of mechanics.json against real Elite Insights 3.32 logs (anonymized and reduced to a few
/// players, see Files/trim_ei_json.py). Elite Insights renames mechanics from time to time and a name that no longer
/// exists silently counts as 0, so every encounter has a fixture.
/// The expected values of every column were calculated independently from the raw json by
/// Files/make_expected_columns.py (expected_columns.json).
/// </summary>
public class BossMechanicsTests
{
    private static readonly string FilesFolder = Path.Combine(AppContext.BaseDirectory, "Files");

    private static JObject Expected => JObject.Parse(File.ReadAllText(Path.Combine(FilesFolder, "expected_columns.json")));

    public static IEnumerable<object[]> Fixtures() => Expected.Properties().Select(p => new object[] { p.Name });

    [Theory]
    [MemberData(nameof(Fixtures))]
    public void ParseFile_RealLog_CountsEveryColumnPerPlayer(string fixture)
    {
        var expected = (JObject)Expected[fixture]!;

        var log = EiJsonParser.ParseFile(Path.Combine(FilesFolder, fixture + ".json"))!;

        Assert.Equal(expected["encounterId"]!.Value<int>(), log.EncounterId);
        Assert.True(log.Killed);
        Assert.Equal(expected["players"]!.Value<int>(), log.Players.Count);
        var columns = (JObject)expected["columns"]!;
        Assert.Equal(columns.Properties().Select(p => p.Name), log.MechanicColumns.Select(c => c.Key));
        foreach (var column in columns.Properties())
        {
            // the message names the failing column, Assert.Equal on sequences does not take one
            var actual = log.Players.Select(p => p.CombinedMechanics[column.Name]).ToList();
            Assert.True(column.Value.Values<int>().SequenceEqual(actual),
                $"column {column.Name} of {fixture}: expected [{string.Join(",", column.Value.Values<int>())}] but was [{string.Join(",", actual)}]");
        }
    }

    [Fact]
    public void Fixtures_CoverAllRaidEncountersWithADefinition()
    {
        var fixtureIds = Expected.Properties().Select(p => p.Value["encounterId"]!.Value<int>()).ToHashSet();

        // one fixture per encounter, the other ids (trio members, second eye, ...) share the definition
        var withoutFixture = Mechanics.MechanicsCatalog.Default.Encounters.Where(e => !e.Ids.Any(fixtureIds.Contains))
            .Select(e => e.Name).ToList();
        Assert.True(withoutFixture.Count == 0,
            "every encounter needs a fixture to notice renamed mechanics: " + string.Join(", ", withoutFixture));
    }

    // Mechanics whose name had to be updated; a fixture that does not show them would not catch a regression.
    [Theory]
    [InlineData("gorseval_kill", "gors_kick")] // Kick.S (was "Kick")
    [InlineData("matthias_kill", "matt_enviroment")] // Tornado.M + Storm (was "Tornado")
    [InlineData("bandit_trio_kill", "trio_tornado")] // Tornado.N (was "Tornado")
    [InlineData("qadim_kill", "qadim_qadim")] // Inf. (was "Inf")
    [InlineData("qadim_peerless_kill", "prlqadim_push")] // Shck.KB + Dome.KB (was "Pushed")
    [InlineData("soulless_horror_kill", "sh_slices")] // 4Slice + 8Slice (was "Slice1"/"Slice2")
    [InlineData("statue_of_ice_kill", "brokenking_cone")] // Cone AoE + Cone Shards (was "Cone Hit")
    [InlineData("cairn_kill", "cairn_green")] // Green.C (was "Green" - "Stab.Green")
    [InlineData("river_of_souls_kill", "river_stun")] // trigger id 19828 (Desmina), not -1
    public void ParseFile_RenamedMechanics_ShowUpInTheFixture(string fixture, string key)
    {
        var log = EiJsonParser.ParseFile(Path.Combine(FilesFolder, fixture + ".json"))!;

        Assert.True(log.Players.Sum(p => p.CombinedMechanics[key]) > 0);
    }

    [Fact]
    public void ParseFile_RealLog_HasNoRealPlayerNames()
    {
        var files = Directory.GetFiles(FilesFolder, "*.json").Where(f => !f.EndsWith("expected_columns.json")).ToList();

        Assert.NotEmpty(files);
        foreach (var file in files)
        {
            Assert.True(EiJsonParser.ParseFile(file)!.Players.All(p => p.AccountName.StartsWith("Player")),
                $"{file} must be anonymized");
        }
    }
}
