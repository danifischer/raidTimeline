using System;
using System.Linq;
using FluentAssertions;
using raidTimeline.Logic.Interfaces;
using raidTimeline.Logic.Mechanics;
using raidTimeline.Models.Models;
using Xunit;

namespace raidTimeline.Logic.Tests;

public class MechanicsFactoryTests
{
    private static readonly Type[] StrategyTypes = typeof(MechanicsFactory).Assembly.GetTypes()
        .Where(t => typeof(IMechanics).IsAssignableFrom(t) && t is { IsAbstract: false, IsInterface: false })
        .ToArray();

    private static string IconOf(Type type) => ((IMechanics)Activator.CreateInstance(type)!).GetEncounterIcon();

    [Fact]
    public void Strategies_AllHaveUniqueNonEmptyEncounterIcon()
    {
        var icons = StrategyTypes.Select(IconOf).ToArray();

        icons.Should().NotBeEmpty().And.OnlyContain(icon => !string.IsNullOrWhiteSpace(icon));
        icons.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Factory_RegistersEveryStrategy()
    {
        var factory = MechanicsFactory.GetMechanicsFactory();

        foreach (var type in StrategyTypes)
        {
            factory.FindStrategy(IconOf(type)).Should().BeOfType(type,
                "{0} must be registered in MechanicsFactory", type.Name);
        }
    }

    [Fact]
    public void FindStrategy_UnknownIcon_ReturnsNull()
    {
        MechanicsFactory.GetMechanicsFactory().FindStrategy("https://example.com/unknown.png").Should().BeNull();
    }

    [Fact]
    public void SoullessHorror_CreateHtml_ListsTopMechanicFailsHtmlEncoded()
    {
        var model = EiHtmlParser.ParseLog(TestFiles.ParsedLog);
        var player = new PlayerModel { AccountName = "<b>x</b>.1234" };
        player.CombinedMechanics["sh_donutIn"] = 0;
        player.CombinedMechanics["sh_donutOut"] = 0;
        player.CombinedMechanics["sh_scythe"] = 99;
        player.CombinedMechanics["sh_slices"] = 0;
        model.Players.Add(player);

        var html = MechanicsFactory.GetMechanicsFactory().FindStrategy(model.EncounterIcon)!.CreateHtml(model);

        html.Should().Contain("mechanicsTable").And.Contain("&lt;b&gt;x&lt;/b&gt;.1234").And.NotContain("<b>x</b>");
    }
}
