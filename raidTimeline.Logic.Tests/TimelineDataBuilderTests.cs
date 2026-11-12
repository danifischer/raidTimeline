using System;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using raidTimeline.Models.Models;
using Xunit;

namespace raidTimeline.Logic.Tests;

public class TimelineDataBuilderTests
{
    private static RaidModel Raid(string name, DateTime start, bool killed = true) => new("log.json")
    {
        EncounterName = name,
        EncounterIcon = "https://example.org/icon.png",
        Killed = killed,
        OccurenceStart = start,
        OccurenceEnd = start.AddMinutes(4)
    };

    private static JsonElement Build(bool newestFirst, params RaidModel[] models) =>
        JsonDocument.Parse(TimelineDataBuilder.Build(models, newestFirst)).RootElement;

    [Fact]
    public void Build_RealLog_ContainsEncounterPlayersBoonsAndMechanics()
    {
        var model = EiJsonParser.ParseFile(TestFiles.ValeGuardianLog)!;

        var log = Build(false, model).GetProperty("logs")[0];

        log.GetProperty("name").GetString().Should().Be("Vale Guardian");
        log.GetProperty("id").GetInt32().Should().Be(15438);
        log.GetProperty("killed").GetBoolean().Should().BeTrue();
        log.GetProperty("url").GetString().Should().Be("vale_guardian_kill.html");
        log.GetProperty("icon").GetString().Should().StartWith("https://render.guildwars2.com/");
        log.GetProperty("hpLeft").EnumerateArray().Select(v => v.GetDouble()).Should().Equal(0d);
        log.GetProperty("columns").EnumerateArray().Select(c => c.GetProperty("label").GetString())
            .Should().Equal("Ports", "Seeker", "Floor");

        var players = log.GetProperty("players");
        players.GetArrayLength().Should().Be(model.Players.Count).And.Be(10);
        var first = players[0];
        first.GetProperty("account").GetString().Should().Be(model.Players[0].AccountName);
        first.GetProperty("dps").GetInt64().Should().Be(model.Players[0].Dps);
        first.GetProperty("buffs").GetArrayLength().Should().Be(model.Players[0].Buffs.Count).And.BeGreaterThan(0);
        first.GetProperty("buffs")[0].GetProperty("name").GetString().Should().Be(model.Players[0].Buffs[0].Name);
        // Keys of the mechanics are written as they are, not renamed to camel case.
        players.EnumerateArray().Any(p => p.GetProperty("mechanics").TryGetProperty("vg_floor", out _)).Should().BeTrue();
    }

    [Fact]
    public void Build_WritesTimesWithoutOffsetSoThePageShowsTheTimesOfTheLog()
    {
        var model = Raid("Gorseval", new DateTime(2026, 11, 9, 21, 1, 38));

        var log = Build(false, model).GetProperty("logs")[0];

        log.GetProperty("start").GetString().Should().Be("2026-11-09T21:01:38");
        log.GetProperty("end").GetString().Should().Be("2026-11-09T21:05:38");
    }

    [Fact]
    public void Build_OrdersTheLogsChronologicallyAndKeepsTheDayOrderFlag()
    {
        var late = Raid("Late", new DateTime(2026, 11, 9, 22, 0, 0));
        var early = Raid("Early", new DateTime(2026, 11, 2, 20, 0, 0));

        var root = Build(true, late, early);

        root.GetProperty("newestFirst").GetBoolean().Should().BeTrue();
        root.GetProperty("logs").EnumerateArray().Select(l => l.GetProperty("name").GetString())
            .Should().Equal("Early", "Late");
        Build(false).GetProperty("newestFirst").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public void Build_NoModels_ProducesAnEmptyList()
    {
        TimelineDataBuilder.Build(Array.Empty<RaidModel>(), false).Should().Be(@"{""newestFirst"":false,""logs"":[]}");
    }

    [Fact]
    public void Build_HostileNames_CannotEndTheScriptTagAndRoundTrip()
    {
        const string name = "</script><img src=x onerror=alert(1)> & \"quote\"";
        var model = Raid(name, new DateTime(2026, 11, 9, 21, 0, 0));
        model.Players.Add(new PlayerModel { AccountName = name, Profession = "<b>Scourge</b>", ProfessionIcon = "" });

        var json = TimelineDataBuilder.Build(new[] { model }, false);

        json.Should().NotContain("<").And.NotContain(">").And.NotContain("&");
        var log = JsonDocument.Parse(json).RootElement.GetProperty("logs")[0];
        log.GetProperty("name").GetString().Should().Be(name);
        log.GetProperty("players")[0].GetProperty("account").GetString().Should().Be(name);
        log.GetProperty("players")[0].GetProperty("profession").GetString().Should().Be("<b>Scourge</b>");
    }
}
