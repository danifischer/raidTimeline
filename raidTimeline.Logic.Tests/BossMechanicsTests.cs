using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace raidTimeline.Logic.Tests;

/// <summary>
/// Runs the boss strategies against real Elite Insights 3.32 logs (anonymized and reduced to three players, see
/// Files/trim_ei_json.py). Elite Insights renamed several mechanics over the years and a name that no longer
/// exists silently counts as 0, so every fixture covers a mechanic whose name had to be updated.
/// The expected counts were calculated independently from the raw json.
/// </summary>
public class BossMechanicsTests
{
    [Theory]
    [InlineData("gorseval_kill", 15429, "gors_kick", "1,1,1")] // Kick.S (was "Kick")
    [InlineData("matthias_kill", 16115, "matt_enviroment", "43,23,30")] // Tornado.M + Storm (was "Tornado")
    [InlineData("bandit_trio_kill", 16088, "trio_tornado", "42,32,37")] // Tornado.N (was "Tornado")
    [InlineData("qadim_kill", 20934, "qadim_qadim", "8,8,17")] // Inf. (was "Inf")
    [InlineData("qadim_peerless_kill", 22000, "prlqadim_push", "2,3,3")] // Shck.KB + Dome.KB (was "Pushed")
    [InlineData("soulless_horror_kill", 19767, "sh_slices", "4,5,5")] // 4Slice + 8Slice (was "Slice1"/"Slice2")
    [InlineData("statue_of_ice_kill", 19691, "brokenking_cone", "6,15,4")] // Cone AoE + Cone Shards (was "Cone Hit")
    [InlineData("cairn_kill", 17194, "cairn_green", "2,1,2")] // Green.C (was "Green" - "Stab.Green")
    [InlineData("river_of_souls_kill", 19828, "river_stun", "2,2,0")] // trigger id 19828 (Desmina)
    public void ParseFile_RealLog_CountsBossMechanicsPerPlayer(string fixture, int encounterId, string key,
        string expectedPerPlayer)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Files", fixture + ".json");

        var log = EiJsonParser.ParseFile(path)!;

        log.EncounterId.Should().Be(encounterId);
        log.Killed.Should().BeTrue();
        log.Players.Select(p => p.CombinedMechanics[key]).Should()
            .Equal(expectedPerPlayer.Split(',').Select(int.Parse));
    }

    [Fact]
    public void ParseFile_RealLog_HasNoRealPlayerNames()
    {
        var files = Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "Files"), "*.json");

        files.Should().NotBeEmpty();
        foreach (var file in files)
        {
            EiJsonParser.ParseFile(file)!.Players.Select(p => p.AccountName)
                .Should().OnlyContain(name => name.StartsWith("Player"), "{0} must be anonymized", file);
        }
    }
}
