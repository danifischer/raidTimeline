using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using raidTimeline.Logic.EiJson;
using raidTimeline.Logic.Mechanics;
using raidTimeline.Models.Models;
using Xunit;

namespace raidTimeline.Logic.Tests;

public class MechanicsApplierTests
{
    private static readonly MechanicsCatalog Catalog = MechanicsCatalog.Parse("""
        {
          "ignoredMechanics": ["Res"],
          "encounters": [
            { "name": "Boss", "ids": [100], "columns": [
                { "key": "slam", "label": "Slam", "title": "Slam hits", "mechanics": ["Slam", "Wave"], "subtract": ["Stab.Slam"] },
                { "key": "tear", "label": "Tear", "title": "Tears", "mechanics": ["Tear"], "rank": false }
            ] }
          ]
        }
        """);

    private static (RaidModel Model, EiLog Log) Create(int triggerId, params (string Mechanic, string Actor, int Times)[] events)
    {
        var log = new EiLog
        {
            TriggerID = triggerId,
            Players =
            {
                new EiPlayer { Account = "A.1111", Name = "Alice" },
                new EiPlayer { Account = "B.2222", Name = "Bob" },
                new EiPlayer { Account = "N.0000", Name = "Sword", FriendlyNPC = true }
            }
        };

        foreach (var group in events.GroupBy(e => e.Mechanic))
        {
            log.Mechanics.Add(new EiMechanic
            {
                Name = group.Key,
                FullName = group.Key + " (full)",
                Description = group.Key + " description",
                MechanicsData = group.SelectMany(e => Enumerable.Range(0, e.Times).Select(_ => new EiMechanicEvent { Actor = e.Actor }))
                    .ToList()
            });
        }

        var model = new RaidModel("test.json");
        model.Players.Add(new PlayerModel { Index = 0, AccountName = "A.1111" });
        model.Players.Add(new PlayerModel { Index = 1, AccountName = "B.2222" });
        return (model, log);
    }

    [Fact]
    public void Apply_KnownEncounter_AddsUpMechanicsAndSubtractsTheOthers()
    {
        var (model, log) = Create(100, ("Slam", "Alice", 5), ("Stab.Slam", "Alice", 2), ("Wave", "Alice", 1),
            ("Wave", "Bob", 4), ("Tear", "Bob", 3));

        MechanicsApplier.Apply(model, log, Catalog);

        model.MechanicColumns.Should().Equal(new MechanicColumn("slam", "Slam", "Slam hits"),
            new MechanicColumn("tear", "Tear", "Tears", false));
        model.Players[0].CombinedMechanics.Should().Contain("slam", 4).And.Contain("tear", 0); // 5 + 1 - 2
        model.Players[1].CombinedMechanics.Should().Contain("slam", 4).And.Contain("tear", 3);
    }

    [Fact]
    public void Apply_KnownEncounter_CountsEveryMechanicPerPlayerEvenWithZero()
    {
        var (model, log) = Create(100, ("Slam", "Alice", 2), ("Other", "Bob", 1));

        MechanicsApplier.Apply(model, log, Catalog);

        model.Players[0].Mechanics.Should().Contain("Slam", 2).And.Contain("Other", 0);
        model.Players[1].Mechanics.Should().Contain("Slam", 0).And.Contain("Other", 1);
    }

    [Fact]
    public void Apply_KnownEncounterWithoutAnyEvents_StillHasTheColumns()
    {
        var (model, log) = Create(100);

        MechanicsApplier.Apply(model, log, Catalog);

        model.MechanicColumns.Select(c => c.Key).Should().Equal("slam", "tear");
        model.Players.Should().OnlyContain(p => p.CombinedMechanics["slam"] == 0);
    }

    [Fact]
    public void Apply_SameMechanicNameTwice_IsAddedUp()
    {
        var (model, log) = Create(100, ("Slam", "Alice", 1));
        log.Mechanics.Add(new EiMechanic { Name = "Slam", MechanicsData = { new EiMechanicEvent { Actor = "Alice" } } });

        MechanicsApplier.Apply(model, log, Catalog);

        model.Players[0].Mechanics["Slam"].Should().Be(2);
    }

    [Fact]
    public void Apply_UnknownEncounter_ShowsTheThreeMechanicsPlayersTriggeredMost()
    {
        var (model, log) = Create(999, ("Floor", "Alice", 4), ("Floor", "Bob", 3), ("Orb", "Alice", 5),
            ("Beam", "Bob", 2), ("Cone", "Alice", 1));

        MechanicsApplier.Apply(model, log, Catalog);

        model.MechanicColumns.Select(c => c.Label).Should().Equal("Floor", "Orb", "Beam");
        model.MechanicColumns[0].Should().Be(new MechanicColumn("gen_Floor", "Floor", "Floor description"));
        model.Players[0].CombinedMechanics.Should().Contain("gen_Floor", 4).And.Contain("gen_Orb", 5);
        model.Players[1].CombinedMechanics.Should().Contain("gen_Floor", 3).And.Contain("gen_Beam", 2);
    }

    [Fact]
    public void Apply_UnknownEncounter_SkipsMechanicsEveryEncounterHasAndEnemyEvents()
    {
        var (model, log) = Create(999, ("Res", "Alice", 9), ("Boss attack", "Training Golem", 20), ("Floor", "Alice", 1));

        MechanicsApplier.Apply(model, log, Catalog);

        model.MechanicColumns.Select(c => c.Label).Should().Equal("Floor");
    }

    [Fact]
    public void Apply_UnknownEncounterWithTiedMechanics_SortsByName()
    {
        var (model, log) = Create(999, ("Zeta", "Alice", 2), ("Alpha", "Alice", 2), ("Mid", "Alice", 2), ("Beta", "Alice", 2));

        MechanicsApplier.Apply(model, log, Catalog);

        model.MechanicColumns.Select(c => c.Label).Should().Equal("Alpha", "Beta", "Mid");
    }

    [Fact]
    public void Apply_UnknownEncounterWithoutPlayerMechanics_HasNoColumns()
    {
        var (model, log) = Create(999, ("Res", "Alice", 3), ("Boss attack", "Training Golem", 20));

        MechanicsApplier.Apply(model, log, Catalog);

        model.MechanicColumns.Should().BeEmpty();
        model.Players.Should().OnlyContain(p => p.CombinedMechanics.Count == 0);
    }

    [Fact]
    public void Apply_UnknownMechanicWithoutDescription_UsesTheFullNameAsTitle()
    {
        var (model, log) = Create(999, ("Floor", "Alice", 1));
        log.Mechanics[0].Description = "";

        MechanicsApplier.Apply(model, log, Catalog);

        model.MechanicColumns.Single().Title.Should().Be("Floor (full)");
    }
}
