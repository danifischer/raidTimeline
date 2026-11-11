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

    private static int[] IdsOf(Type type) => ((IMechanics)Activator.CreateInstance(type)!).GetEncounterIds().ToArray();

    [Fact]
    public void Strategies_AllHaveEncounterIds_WhichAreUniqueAcrossStrategies()
    {
        var ids = StrategyTypes.SelectMany(IdsOf).ToArray();

        StrategyTypes.Should().OnlyContain(t => IdsOf(t).Length > 0);
        ids.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Factory_RegistersEveryStrategy()
    {
        var factory = MechanicsFactory.GetMechanicsFactory();

        foreach (var type in StrategyTypes)
        {
            foreach (var id in IdsOf(type))
            {
                factory.FindStrategy(id).Should().BeOfType(type,
                    "{0} must be registered in MechanicsFactory", type.Name);
            }
        }
    }

    [Fact]
    public void FindStrategy_UnknownEncounter_ReturnsNull()
    {
        MechanicsFactory.GetMechanicsFactory().FindStrategy(123456).Should().BeNull();
    }

    [Fact]
    public void ValeGuardian_CreateHtml_ListsTopMechanicFailsHtmlEncoded()
    {
        var model = EiJsonParser.ParseFile(TestFiles.ValeGuardianLog)!;
        var player = new PlayerModel { AccountName = "<b>x</b>.1234" };
        player.CombinedMechanics["vg_tp"] = 0;
        player.CombinedMechanics["vg_seeker"] = 99;
        player.CombinedMechanics["vg_floor"] = 0;
        model.Players.Add(player);

        var html = MechanicsFactory.GetMechanicsFactory().FindStrategy(model.EncounterId)!.CreateHtml(model);

        html.Should().Contain("mechanicsTable").And.Contain("&lt;b&gt;x&lt;/b&gt;.1234").And.NotContain("<b>x</b>");
    }
}
