using System;
using System.Globalization;
using System.IO;
using System.Linq;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using raidTimeline.Models.Models;
using Xunit;

namespace raidTimeline.Logic.Tests;

public class EiHtmlParserTests
{
    private static readonly DateTime ExpectedOccurenceStart = DateTime.ParseExact("2021-07-15 20:14:12 +02:00",
        "yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture);

    private static readonly DateTime ExpectedOccurenceEnd = DateTime.ParseExact("2021-07-15 20:18:22 +02:00",
        "yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture);

    [Fact]
    public void ParseLog_Default_HasExpectedEncounterValues()
    {
        var log = EiHtmlParser.ParseLog(TestFiles.ParsedLog);

        log.Should().NotBeNull();
        log.EncounterName.Should().Be("Soulless Horror");
        log.EncounterId.Should().Be(19767);
        log.EncounterTime.Should().Be("03m 57s 362ms");
        log.EncounterIcon.Should().Be("https://wiki.guildwars2.com/images/d/d4/Mini_Desmina.png");
        log.Killed.Should().BeTrue();
        log.HpLeft.Should().Equal(0d);
        log.OccurenceStart.Should().Be(ExpectedOccurenceStart);
        log.OccurenceEnd.Should().Be(ExpectedOccurenceEnd);
        log.LogPath.Should().Be(TestFiles.ParsedLog);
        log.LogUrl.Should().Be("parsed_test_log.html");
    }

    [Fact]
    public void ParseLog_Default_ParsesAllPlayers()
    {
        var log = EiHtmlParser.ParseLog(TestFiles.ParsedLog);

        log.Players.Should().HaveCount(10);
        log.Players.Select(p => p.Index).Should().Equal(Enumerable.Range(0, 10));
        log.Players.Select(p => p.AccountName).Should().OnlyHaveUniqueItems();
        log.Players.Should().OnlyContain(p => p.Damage > 0 && p.Dps > 0);
    }

    [Fact]
    public void ParseLog_Default_ParsesFirstPlayerStats()
    {
        var player = EiHtmlParser.ParseLog(TestFiles.ParsedLog).Players[0];

        player.AccountName.Should().Be("Max Zero.2167");
        player.Profession.Should().Be("Druid");
        player.Group.Should().Be(1);
        player.Damage.Should().Be(875438);
        player.Dps.Should().Be(3688);
        player.Cc.Should().Be(764);
        player.ResAmount.Should().Be(0);
        player.TimesDowned.Should().Be(0);
        player.TimesDied.Should().Be(0);
        player.DiedText.Should().Be("100% Alive");
    }

    [Fact]
    public void ParseLog_Default_ParsesBuffsInLogOrder()
    {
        var buffs = EiHtmlParser.ParseLog(TestFiles.ParsedLog).Players[0].Buffs;

        buffs.Select(b => b.Name).Take(4).Should().Equal("Might", "Fury", "Quickness", "Alacrity");
        buffs.Select(b => b.Index).Should().BeInAscendingOrder();

        var might = buffs[0];
        might.Id.Should().Be("740");
        might.Stacking.Should().BeTrue();
        might.Value.Should().BeApproximately(23.698, 0.0001);
        might.Stacks.Should().BeApproximately(96.958, 0.0001);

        var fury = buffs[1];
        fury.Stacking.Should().BeFalse();
        fury.Stacks.Should().BeNull();
    }

    [Fact]
    public void ParseLog_SoullessHorror_ParsesBossMechanics()
    {
        var player = EiHtmlParser.ParseLog(TestFiles.ParsedLog).Players[0];

        player.Mechanics.Should().Contain("Scythe", 2).And.Contain("8Slice", 3).And.Contain("Spin", 7);
        player.CombinedMechanics.Should().Contain("sh_scythe", 2).And.Contain("sh_slices", 3);
    }

    [Fact]
    public void ParseLog_NewerBoonStatsLocation_ParsesSameAsOlderFormat()
    {
        using var temp = TestFiles.CreateTempDirectory();
        // Newer Elite Insights versions moved boonStats into buffsStatContainer.
        var path = TestFiles.WriteModifiedLog(Path.Combine(temp.Path, "new_format.html"), json =>
        {
            foreach (var phase in json["phases"]!)
            {
                var boonStats = phase["boonStats"]!;
                ((JObject)phase).Remove("boonStats");
                phase["buffsStatContainer"] = new JObject { ["boonStats"] = boonStats };
            }
        });

        var expected = EiHtmlParser.ParseLog(TestFiles.ParsedLog);
        var actual = EiHtmlParser.ParseLog(path);

        actual.Should().NotBeNull();
        actual.Players.Select(p => p.Buffs.Select(b => (b.Name, b.Value, b.Stacks)))
            .Should().BeEquivalentTo(expected.Players.Select(p => p.Buffs.Select(b => (b.Name, b.Value, b.Stacks))));
    }

    [Fact]
    public void ParseLog_FileWithoutLogData_ReturnsNull()
    {
        using var temp = TestFiles.CreateTempDirectory();
        var path = Path.Combine(temp.Path, "empty.html");
        File.WriteAllText(path, "<html><body>nothing to see</body></html>");

        EiHtmlParser.ParseLog(path).Should().BeNull();
    }

    [Fact]
    public void ParseLog_BrokenLogData_ReturnsNull()
    {
        using var temp = TestFiles.CreateTempDirectory();
        var path = Path.Combine(temp.Path, "broken.html");
        File.WriteAllText(path, "<script>var _logData = {\"phases\":[]};</script>");

        EiHtmlParser.ParseLog(path).Should().BeNull();
    }

    [Fact]
    public void ParseLog_MissingFile_ReturnsNull()
    {
        EiHtmlParser.ParseLog(Path.Combine(AppContext.BaseDirectory, "does-not-exist.html")).Should().BeNull();
    }
}
