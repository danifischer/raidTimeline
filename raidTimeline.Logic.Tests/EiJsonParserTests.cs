using System;
using System.Globalization;
using System.IO;
using System.Linq;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace raidTimeline.Logic.Tests;

/// <summary>
/// Expected values were calculated independently from the raw Elite Insights json of the fixture.
/// </summary>
public class EiJsonParserTests
{
    private static readonly DateTime ExpectedStart = DateTime.ParseExact("2020-08-01 21:15:18 +02:00",
        "yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture);

    private static readonly DateTime ExpectedEnd = DateTime.ParseExact("2020-08-01 21:19:20 +02:00",
        "yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture);

    [Fact]
    public void ParseFile_ValeGuardianKill_HasExpectedEncounterValues()
    {
        var log = EiJsonParser.ParseFile(TestFiles.ValeGuardianLog);

        log.Should().NotBeNull();
        log!.EncounterName.Should().Be("Vale Guardian");
        log.EncounterId.Should().Be(15438);
        log.EncounterTime.Should().Be("03m 59s 777ms");
        log.EncounterIcon.Should().StartWith("https://render.guildwars2.com/");
        log.Killed.Should().BeTrue();
        log.OccurenceStart.Should().Be(ExpectedStart);
        log.OccurenceEnd.Should().Be(ExpectedEnd);
        log.LogPath.Should().Be(TestFiles.ValeGuardianLog);
    }

    [Fact]
    public void ParseFile_Default_LinksToHtmlNextToJson()
    {
        EiJsonParser.ParseFile(TestFiles.ValeGuardianLog)!.LogUrl.Should().Be("vale_guardian_kill.html");
    }

    [Fact]
    public void ParseFile_ExplicitLogUrl_IsUsed()
    {
        EiJsonParser.ParseFile(TestFiles.ValeGuardianLog, logUrl: "https://dps.report/abcd")!
            .LogUrl.Should().Be("https://dps.report/abcd");
    }

    [Fact]
    public void ParseFile_ValeGuardianKill_OnlyMainTargetCountsAsBossHealth()
    {
        // The log has 7 targets, but six of them are the blocking guardians.
        var log = EiJsonParser.ParseFile(TestFiles.ValeGuardianLog)!;

        log.HpLeft.Should().Equal(0d);
    }

    [Fact]
    public void ParseFile_ValeGuardianKill_ParsesAllPlayers()
    {
        var log = EiJsonParser.ParseFile(TestFiles.ValeGuardianLog)!;

        log.Players.Should().HaveCount(10);
        log.Players.Select(p => p.Index).Should().Equal(Enumerable.Range(0, 10));
        log.Players.Select(p => p.AccountName).Should().OnlyHaveUniqueItems();
        log.Players.Select(p => p.Group).Should().Equal(1, 1, 1, 1, 1, 2, 2, 2, 2, 2);
        log.Players.Should().OnlyContain(p => p.Damage > 0 && p.Dps > 0 && !p.IsNpc);
    }

    [Theory]
    [InlineData(0, "Player1.1001", "Scourge", 267732, 1116)]
    [InlineData(2, "Player3.1003", "Holosmith", 4253160, 17737)]
    [InlineData(8, "Player9.1009", "Druid", 476294, 1986)]
    public void ParseFile_ValeGuardianKill_ParsesPlayerDamage(int index, string account, string profession,
        long damage, long dps)
    {
        var player = EiJsonParser.ParseFile(TestFiles.ValeGuardianLog)!.Players[index];

        player.AccountName.Should().Be(account);
        player.Profession.Should().Be(profession);
        player.ProfessionIcon.Should().StartWith("https://render.guildwars2.com/");
        player.Damage.Should().Be(damage);
        player.Dps.Should().Be(dps);
    }

    [Fact]
    public void ParseFile_ValeGuardianKill_ParsesResurrectionsAndDowns()
    {
        var players = EiJsonParser.ParseFile(TestFiles.ValeGuardianLog)!.Players;

        players[7].ResAmount.Should().Be(1);
        players[7].ResTime.Should().BeApproximately(2.564, 0.0001);
        players[2].ResAmount.Should().Be(0);
        players[5].TimesDowned.Should().Be(1);
        players[8].TimesDowned.Should().Be(1);
        players.Sum(p => p.TimesDowned).Should().Be(2);
        players.Sum(p => p.TimesDied).Should().Be(0);
        players.Sum(p => p.ResAmount).Should().Be(5);
    }

    [Fact]
    public void ParseFile_ValeGuardianKill_ParsesBoonsInDisplayOrder()
    {
        var buffs = EiJsonParser.ParseFile(TestFiles.ValeGuardianLog)!.Players[0].Buffs;

        buffs.Select(b => b.Name).Should().Equal("Might", "Fury", "Quickness", "Alacrity", "Protection",
            "Regeneration", "Vigor", "Aegis", "Stability", "Swiftness", "Resistance", "Retaliation");
        buffs.Select(b => b.Index).Should().Equal(Enumerable.Range(0, 12));
        buffs.Should().OnlyContain(b => b.Icon.StartsWith("https://render.guildwars2.com/"));
    }

    [Fact]
    public void ParseFile_ValeGuardianKill_ParsesStackingAndNonStackingBoonValues()
    {
        var buffs = EiJsonParser.ParseFile(TestFiles.ValeGuardianLog)!.Players[0].Buffs;

        var might = buffs.Single(b => b.Id == "740");
        might.Stacking.Should().BeTrue();
        might.Value.Should().BeApproximately(18.123, 0.0001);
        might.Stacks.Should().BeApproximately(96.418, 0.0001);

        var quickness = buffs.Single(b => b.Id == "1187");
        quickness.Stacking.Should().BeFalse();
        quickness.Value.Should().BeApproximately(95.764, 0.0001);
        quickness.Stacks.Should().BeNull();
    }

    [Fact]
    public void ParseFile_BoonWithoutUptimeData_IsZero()
    {
        // Shihaya has no recorded vigor uptime
        var vigor = EiJsonParser.ParseFile(TestFiles.ValeGuardianLog)!.Players[0].Buffs.Single(b => b.Name == "Vigor");

        vigor.Value.Should().Be(0);
    }

    [Fact]
    public void ParseFile_ValeGuardianKill_CountsMechanicsPerPlayerByCharacterName()
    {
        var players = EiJsonParser.ParseFile(TestFiles.ValeGuardianLog)!.Players;

        players[0].Mechanics.Should().Contain("Seeker", 3).And.Contain("Floor R", 4).And.Contain("Orbs", 3);
        players[5].Mechanics.Should().Contain("Seeker", 5).And.Contain("Downed", 1).And.Contain("Split TP", 1);
        players[6].Mechanics.Should().Contain("Floor R", 11).And.Contain("Floor G", 9);

        // every mechanic of the log is present for every player, even with a count of 0
        players.Select(p => p.Mechanics.Count).Distinct().Should().ContainSingle().Which.Should().Be(22);
        players[9].Mechanics.Should().Contain("Res", 0);
    }

    [Fact]
    public void ParseFile_ValeGuardianKill_CombinesMechanicsForTheBossTable()
    {
        var players = EiJsonParser.ParseFile(TestFiles.ValeGuardianLog)!.Players;

        players[0].CombinedMechanics.Should().Contain("vg_tp", 2).And.Contain("vg_seeker", 3)
            .And.Contain("vg_floor", 7);
        players[5].CombinedMechanics.Should().Contain("vg_tp", 3).And.Contain("vg_seeker", 5);
    }

    [Fact]
    public void ParseFile_UnknownEncounter_ParsesWithoutBossMechanics()
    {
        using var temp = TestFiles.CreateTempDirectory();
        var path = TestFiles.WriteModifiedLog(Path.Combine(temp.Path, "unknown.json"), json => json["triggerID"] = 123456);

        var log = EiJsonParser.ParseFile(path)!;

        log.Players.Should().HaveCount(10);
        log.Players.Should().OnlyContain(p => p.Mechanics.Count == 0 && p.CombinedMechanics.Count == 0);
    }

    [Fact]
    public void ParseFile_UnknownProfession_HasNoIcon()
    {
        using var temp = TestFiles.CreateTempDirectory();
        var path = TestFiles.WriteModifiedLog(Path.Combine(temp.Path, "profession.json"),
            json => json["players"]![0]!["profession"] = "Brand New Spec");

        var player = EiJsonParser.ParseFile(path)!.Players[0];

        player.Profession.Should().Be("Brand New Spec");
        player.ProfessionIcon.Should().BeEmpty();
    }

    [Fact]
    public void ParseFile_NoMainTargetPriority_FallsBackToFirstTarget()
    {
        using var temp = TestFiles.CreateTempDirectory();
        var path = TestFiles.WriteModifiedLog(Path.Combine(temp.Path, "priorities.json"), json =>
            json["phases"]![0]!["targetPriorities"] = new JObject { ["0"] = "BLOCKING" });

        var log = EiJsonParser.ParseFile(path)!;

        log.HpLeft.Should().HaveCount(1);
        log.Players[0].Damage.Should().Be(267732);
    }

    [Fact]
    public void ParseFile_NotInSquadPlayer_IsMarkedAsNpc()
    {
        using var temp = TestFiles.CreateTempDirectory();
        var path = TestFiles.WriteModifiedLog(Path.Combine(temp.Path, "npc.json"),
            json => json["players"]![3]!["notInSquad"] = true);

        EiJsonParser.ParseFile(path)!.Players.Select(p => p.IsNpc).Should()
            .Equal(false, false, false, true, false, false, false, false, false, false);
    }

    [Fact]
    public void ParseFile_FriendlyNpc_IsLeftOutButOtherPlayersKeepTheirMechanics()
    {
        // Elite Insights lists friendly npcs and gadgets (e.g. a Conjured Sword) next to the players.
        using var temp = TestFiles.CreateTempDirectory();
        var path = TestFiles.WriteModifiedLog(Path.Combine(temp.Path, "npc.json"),
            json => json["players"]![3]!["friendlyNPC"] = true);

        var players = EiJsonParser.ParseFile(path)!.Players;

        players.Should().HaveCount(9);
        players.Select(p => p.AccountName).Should().NotContain("Player4.1004");
        players.Select(p => p.Index).Should().Equal(0, 1, 2, 4, 5, 6, 7, 8, 9);
        // Player5.1005 is the player behind the removed one in the log, the mechanics still have to be his
        players.Single(p => p.AccountName == "Player5.1005").Mechanics.Should().Contain("Floor B", 7);
    }

    [Fact]
    public void ParseFile_FailedAttempt_IsNotKilled()
    {
        using var temp = TestFiles.CreateTempDirectory();
        var path = TestFiles.WriteModifiedLog(Path.Combine(temp.Path, "wipe.json"), json =>
        {
            json["success"] = false;
            json["targets"]![0]!["healthPercentBurned"] = 62.5;
        });

        var log = EiJsonParser.ParseFile(path)!;

        log.Killed.Should().BeFalse();
        log.HpLeft.Should().Equal(37.5);
    }

    [Fact]
    public void ParseJson_ValidJson_UsesGivenSourceAndLink()
    {
        var json = File.ReadAllText(TestFiles.ValeGuardianLog);

        var log = EiJsonParser.ParseJson(json, "dps.report", "https://dps.report/abcd");

        log!.LogPath.Should().Be("dps.report");
        log.LogUrl.Should().Be("https://dps.report/abcd");
        log.Players.Should().HaveCount(10);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("{}")]
    [InlineData("{\"players\":[],\"phases\":[]}")]
    [InlineData("<html><body>an html log</body></html>")]
    public void ParseJson_NotAnEliteInsightsLog_ReturnsNull(string content)
    {
        EiJsonParser.ParseJson(content, "source", "link").Should().BeNull();
    }

    [Fact]
    public void ParseFile_MissingFile_ReturnsNull()
    {
        EiJsonParser.ParseFile(Path.Combine(AppContext.BaseDirectory, "does-not-exist.json")).Should().BeNull();
    }

    [Fact]
    public void ParseFile_InvalidDate_ReturnsNull()
    {
        using var temp = TestFiles.CreateTempDirectory();
        var path = TestFiles.WriteModifiedLog(Path.Combine(temp.Path, "date.json"),
            json => json["timeStartStd"] = "yesterday");

        EiJsonParser.ParseFile(path).Should().BeNull();
    }
}
