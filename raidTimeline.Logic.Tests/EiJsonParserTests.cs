using System;
using System.Globalization;
using System.IO;
using System.Linq;
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

        Assert.NotNull(log);
        Assert.Equal("Vale Guardian", log.EncounterName);
        Assert.Equal(15438, log.EncounterId);
        Assert.Equal("03m 59s 777ms", log.EncounterTime);
        Assert.StartsWith("https://render.guildwars2.com/", log.EncounterIcon);
        Assert.True(log.Killed);
        Assert.Equal(ExpectedStart, log.OccurenceStart);
        Assert.Equal(ExpectedEnd, log.OccurenceEnd);
        Assert.Equal(TestFiles.ValeGuardianLog, log.LogPath);
    }

    [Fact]
    public void ParseFile_Default_LinksToHtmlNextToJson()
    {
        Assert.Equal("vale_guardian_kill.html", EiJsonParser.ParseFile(TestFiles.ValeGuardianLog)!.LogUrl);
    }

    [Fact]
    public void ParseFile_ExplicitLogUrl_IsUsed()
    {
        Assert.Equal("https://dps.report/abcd",
            EiJsonParser.ParseFile(TestFiles.ValeGuardianLog, logUrl: "https://dps.report/abcd")!.LogUrl);
    }

    [Fact]
    public void ParseFile_ValeGuardianKill_OnlyMainTargetCountsAsBossHealth()
    {
        // The log has 7 targets, but six of them are the blocking guardians.
        var log = EiJsonParser.ParseFile(TestFiles.ValeGuardianLog)!;

        Assert.Equal(new[] { 0d }, log.HpLeft);
    }

    [Fact]
    public void ParseFile_ValeGuardianKill_ParsesAllPlayers()
    {
        var log = EiJsonParser.ParseFile(TestFiles.ValeGuardianLog)!;

        Assert.Equal(10, log.Players.Count);
        Assert.Equal(Enumerable.Range(0, 10), log.Players.Select(p => p.Index));
        Assert.Equal(10, log.Players.Select(p => p.AccountName).Distinct().Count());
        Assert.Equal(new[] { 1, 1, 1, 1, 1, 2, 2, 2, 2, 2 }, log.Players.Select(p => p.Group));
        Assert.All(log.Players, p =>
        {
            Assert.True(p.Damage > 0);
            Assert.True(p.Dps > 0);
            Assert.False(p.IsNpc);
        });
    }

    [Theory]
    [InlineData(0, "Player1.1001", "Scourge", 267732, 1116)]
    [InlineData(2, "Player3.1003", "Holosmith", 4253160, 17737)]
    [InlineData(8, "Player9.1009", "Druid", 476294, 1986)]
    public void ParseFile_ValeGuardianKill_ParsesPlayerDamage(int index, string account, string profession,
        long damage, long dps)
    {
        var player = EiJsonParser.ParseFile(TestFiles.ValeGuardianLog)!.Players[index];

        Assert.Equal(account, player.AccountName);
        Assert.Equal(profession, player.Profession);
        Assert.StartsWith("https://render.guildwars2.com/", player.ProfessionIcon);
        Assert.Equal(damage, player.Damage);
        Assert.Equal(dps, player.Dps);
    }

    [Fact]
    public void ParseFile_ValeGuardianKill_ParsesResurrectionsAndDowns()
    {
        var players = EiJsonParser.ParseFile(TestFiles.ValeGuardianLog)!.Players;

        Assert.Equal(1, players[7].ResAmount);
        Assert.Equal(2.564, players[7].ResTime, 4);
        Assert.Equal(0, players[2].ResAmount);
        Assert.Equal(1, players[5].TimesDowned);
        Assert.Equal(1, players[8].TimesDowned);
        Assert.Equal(2, players.Sum(p => p.TimesDowned));
        Assert.Equal(0, players.Sum(p => p.TimesDied));
        Assert.Equal(5, players.Sum(p => p.ResAmount));
    }

    [Fact]
    public void ParseFile_ValeGuardianKill_ParsesBoonsInDisplayOrder()
    {
        var buffs = EiJsonParser.ParseFile(TestFiles.ValeGuardianLog)!.Players[0].Buffs;

        Assert.Equal(new[]
        {
            "Might", "Fury", "Quickness", "Alacrity", "Protection", "Regeneration", "Vigor", "Aegis", "Stability",
            "Swiftness", "Resistance", "Retaliation"
        }, buffs.Select(b => b.Name));
        Assert.Equal(Enumerable.Range(0, 12), buffs.Select(b => b.Index));
        Assert.All(buffs, b => Assert.StartsWith("https://render.guildwars2.com/", b.Icon));
    }

    [Fact]
    public void ParseFile_ValeGuardianKill_ParsesStackingAndNonStackingBoonValues()
    {
        var buffs = EiJsonParser.ParseFile(TestFiles.ValeGuardianLog)!.Players[0].Buffs;

        var might = buffs.Single(b => b.Id == "740");
        Assert.True(might.Stacking);
        Assert.Equal(18.123, might.Value, 4);
        Assert.Equal(96.418, might.Stacks!.Value, 4);

        var quickness = buffs.Single(b => b.Id == "1187");
        Assert.False(quickness.Stacking);
        Assert.Equal(95.764, quickness.Value, 4);
        Assert.Null(quickness.Stacks);
    }

    [Fact]
    public void ParseFile_BoonWithoutUptimeData_IsZero()
    {
        // Shihaya has no recorded vigor uptime
        var vigor = EiJsonParser.ParseFile(TestFiles.ValeGuardianLog)!.Players[0].Buffs.Single(b => b.Name == "Vigor");

        Assert.Equal(0, vigor.Value);
    }

    [Fact]
    public void ParseFile_ValeGuardianKill_CountsMechanicsPerPlayerByCharacterName()
    {
        var players = EiJsonParser.ParseFile(TestFiles.ValeGuardianLog)!.Players;

        TestFiles.AssertCounts(players[0].Mechanics, ("Seeker", 3), ("Floor R", 4), ("Orbs", 3));
        TestFiles.AssertCounts(players[5].Mechanics, ("Seeker", 5), ("Downed", 1), ("Split TP", 1));
        TestFiles.AssertCounts(players[6].Mechanics, ("Floor R", 11), ("Floor G", 9));

        // every mechanic of the log is present for every player, even with a count of 0
        Assert.Equal(22, Assert.Single(players.Select(p => p.Mechanics.Count).Distinct()));
        TestFiles.AssertCounts(players[9].Mechanics, ("Res", 0));
    }

    [Fact]
    public void ParseFile_ValeGuardianKill_CombinesMechanicsForTheBossTable()
    {
        var players = EiJsonParser.ParseFile(TestFiles.ValeGuardianLog)!.Players;

        TestFiles.AssertCounts(players[0].CombinedMechanics, ("vg_tp", 2), ("vg_seeker", 3), ("vg_floor", 7));
        TestFiles.AssertCounts(players[5].CombinedMechanics, ("vg_tp", 3), ("vg_seeker", 5));
    }

    [Fact]
    public void ParseFile_ValeGuardianKill_UsesTheColumnsOfTheEncounter()
    {
        var log = EiJsonParser.ParseFile(TestFiles.ValeGuardianLog)!;

        Assert.Equal(new[] { "Ports", "Seeker", "Floor" }, log.MechanicColumns.Select(c => c.Label));
        Assert.Equal(new[] { "vg_tp", "vg_seeker", "vg_floor" }, log.MechanicColumns.Select(c => c.Key));
    }

    [Fact]
    public void ParseFile_UnknownEncounter_ShowsTheMechanicsPlayersTriggeredMost()
    {
        using var temp = TestFiles.CreateTempDirectory();
        var path = TestFiles.WriteModifiedLog(Path.Combine(temp.Path, "unknown.json"), json => json["triggerID"] = 123456);

        var log = EiJsonParser.ParseFile(path)!;

        Assert.Equal(10, log.Players.Count);
        // Miss.Qck, Miss.Alac, Downed, Res, ... are ignored; Orbs 35 times, Floor R and Seeker 26 times each
        Assert.Equal(new[] { "Orbs", "Floor R", "Seeker" }, log.MechanicColumns.Select(c => c.Label));
        Assert.Equal("Bullet Storm (Orbs during split)", log.MechanicColumns[0].Title);
        Assert.Equal(35, log.Players.Select(p => p.CombinedMechanics["gen_Orbs"]).Sum());
        TestFiles.AssertCounts(log.Players[0].Mechanics, ("Seeker", 3), ("Orbs", 3));
    }

    [Fact]
    public void ParseFile_UnknownProfession_HasNoIcon()
    {
        using var temp = TestFiles.CreateTempDirectory();
        var path = TestFiles.WriteModifiedLog(Path.Combine(temp.Path, "profession.json"),
            json => json["players"]![0]!["profession"] = "Brand New Spec");

        var player = EiJsonParser.ParseFile(path)!.Players[0];

        Assert.Equal("Brand New Spec", player.Profession);
        Assert.Empty(player.ProfessionIcon);
    }

    [Fact]
    public void ParseFile_NoMainTargetPriority_FallsBackToFirstTarget()
    {
        using var temp = TestFiles.CreateTempDirectory();
        var path = TestFiles.WriteModifiedLog(Path.Combine(temp.Path, "priorities.json"), json =>
            json["phases"]![0]!["targetPriorities"] = new JObject { ["0"] = "BLOCKING" });

        var log = EiJsonParser.ParseFile(path)!;

        Assert.Single(log.HpLeft);
        Assert.Equal(267732, log.Players[0].Damage);
    }

    [Fact]
    public void ParseFile_NotInSquadPlayer_IsMarkedAsNpc()
    {
        using var temp = TestFiles.CreateTempDirectory();
        var path = TestFiles.WriteModifiedLog(Path.Combine(temp.Path, "npc.json"),
            json => json["players"]![3]!["notInSquad"] = true);

        Assert.Equal(new[] { false, false, false, true, false, false, false, false, false, false },
            EiJsonParser.ParseFile(path)!.Players.Select(p => p.IsNpc));
    }

    [Fact]
    public void ParseFile_FriendlyNpc_IsLeftOutButOtherPlayersKeepTheirMechanics()
    {
        // Elite Insights lists friendly npcs and gadgets (e.g. a Conjured Sword) next to the players.
        using var temp = TestFiles.CreateTempDirectory();
        var path = TestFiles.WriteModifiedLog(Path.Combine(temp.Path, "npc.json"),
            json => json["players"]![3]!["friendlyNPC"] = true);

        var players = EiJsonParser.ParseFile(path)!.Players;

        Assert.Equal(9, players.Count);
        Assert.DoesNotContain("Player4.1004", players.Select(p => p.AccountName));
        Assert.Equal(new[] { 0, 1, 2, 4, 5, 6, 7, 8, 9 }, players.Select(p => p.Index));
        // Player5.1005 is the player behind the removed one in the log, the mechanics still have to be his
        TestFiles.AssertCounts(players.Single(p => p.AccountName == "Player5.1005").Mechanics, ("Floor B", 7));
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

        Assert.False(log.Killed);
        Assert.Equal(new[] { 37.5 }, log.HpLeft);
    }

    [Fact]
    public void ParseJson_ValidJson_UsesGivenSourceAndLink()
    {
        var json = File.ReadAllText(TestFiles.ValeGuardianLog);

        var log = EiJsonParser.ParseJson(json, "dps.report", "https://dps.report/abcd");

        Assert.NotNull(log);
        Assert.Equal("dps.report", log.LogPath);
        Assert.Equal("https://dps.report/abcd", log.LogUrl);
        Assert.Equal(10, log.Players.Count);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("{}")]
    [InlineData("{\"players\":[],\"phases\":[]}")]
    [InlineData("<html><body>an html log</body></html>")]
    public void ParseJson_NotAnEliteInsightsLog_ReturnsNull(string content)
    {
        Assert.Null(EiJsonParser.ParseJson(content, "source", "link"));
    }

    [Fact]
    public void ParseFile_MissingFile_ReturnsNull()
    {
        Assert.Null(EiJsonParser.ParseFile(Path.Combine(AppContext.BaseDirectory, "does-not-exist.json")));
    }

    [Fact]
    public void ParseFile_InvalidDate_ReturnsNull()
    {
        using var temp = TestFiles.CreateTempDirectory();
        var path = TestFiles.WriteModifiedLog(Path.Combine(temp.Path, "date.json"),
            json => json["timeStartStd"] = "yesterday");

        Assert.Null(EiJsonParser.ParseFile(path));
    }
}
