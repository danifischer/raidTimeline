using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using FluentAssertions;
using raidTimeline.Logic.HtmlBuilders;
using raidTimeline.Models.Models;
using Xunit;

namespace raidTimeline.Logic.Tests;

public class MechanicsTableBuilderTests
{
    private static RaidModel CreateModel(params MechanicColumn[] columns)
    {
        var model = new RaidModel("test.json");
        model.MechanicColumns.AddRange(columns);
        return model;
    }

    private static PlayerModel Player(string account, params (string Key, int Value)[] values)
    {
        var player = new PlayerModel { AccountName = account };
        foreach (var (key, value) in values) player.CombinedMechanics[key] = value;
        return player;
    }

    private static string Build(RaidModel model) => new StringBuilder().BuildMechanicsTable(model).ToString();

    private static string[] Rows(string html) =>
        Regex.Matches(html, "<tr><td>(.*?)</td>").Select(m => m.Groups[1].Value).ToArray();

    [Fact]
    public void BuildMechanicsTable_NoColumns_AddsNothing()
    {
        var model = CreateModel();
        model.Players.Add(Player("A.1111"));

        Build(model).Should().BeEmpty();
    }

    [Fact]
    public void BuildMechanicsTable_ShowsTheThreePlayersWithTheMostFails()
    {
        var model = CreateModel(new MechanicColumn("a", "A", "Column A"), new MechanicColumn("b", "B", "Column B"));
        model.Players.Add(Player("one.1", ("a", 1), ("b", 0)));
        model.Players.Add(Player("two.2", ("a", 5), ("b", 5)));
        model.Players.Add(Player("three.3", ("a", 0), ("b", 0)));
        model.Players.Add(Player("four.4", ("a", 3), ("b", 1)));

        var html = Build(model);

        Rows(html).Should().Equal("two.2", "four.4", "one.1");
        html.Should().Contain("<th>Player</th>")
            .And.Contain(@"<th title=""Column A"">A</th>").And.Contain(@"<th title=""Column B"">B</th>")
            .And.Contain("<tr><td>two.2</td><td>5</td><td>5</td></tr>")
            .And.Contain(@"<table class=""mechanicsTable"" style=""display: none;"">");
    }

    [Fact]
    public void BuildMechanicsTable_ColumnsWithoutRank_AreShownButDoNotDecideTheOrder()
    {
        var model = CreateModel(new MechanicColumn("fail", "Fail", "Fail"), new MechanicColumn("tear", "Tear", "Tear", false));
        model.Players.Add(Player("collector.1", ("fail", 1), ("tear", 50)));
        model.Players.Add(Player("clumsy.2", ("fail", 4), ("tear", 0)));

        var html = Build(model);

        Rows(html).Should().Equal("clumsy.2", "collector.1");
        html.Should().Contain("<tr><td>collector.1</td><td>1</td><td>50</td></tr>");
    }

    [Fact]
    public void BuildMechanicsTable_PlayerWithoutValueForAColumn_ShowsZero()
    {
        var model = CreateModel(new MechanicColumn("a", "A", "A"));
        model.Players.Add(Player("late.1"));

        Build(model).Should().Contain("<tr><td>late.1</td><td>0</td></tr>");
    }

    [Fact]
    public void BuildMechanicsTable_HtmlEncodesNamesLabelsAndTitles()
    {
        var model = CreateModel(new MechanicColumn("a", "<i>A</i>", "Say \"hi\" & <b>bye</b>"));
        model.Players.Add(Player("<script>alert(1)</script>.1", ("a", 1)));

        var html = Build(model);

        html.Should().NotContain("<script>").And.NotContain("<i>").And.NotContain("<b>");
        html.Should().Contain("&lt;script&gt;").And.Contain("&lt;i&gt;A&lt;/i&gt;").And.Contain("&amp;");
        html.Should().NotContain("title=\"Say \"hi\"");
    }

    [Fact]
    public void BuildMechanicsTable_RealLog_ShowsTheWorstPlayersOfTheEncounter()
    {
        var model = EiJsonParser.ParseFile(TestFiles.ValeGuardianLog)!;

        var html = Build(model);

        html.Should().Contain(@"title=""Unstable Magic Spike (Green Guard or Boss Teleport)"">Ports</th>");
        // Player7 (index 6) triggered 11 + 9 floor hits, Player6 5 seekers and 3 ports, ...
        Rows(html).Should().HaveCount(3).And.StartWith("Player7.1007");
    }
}
