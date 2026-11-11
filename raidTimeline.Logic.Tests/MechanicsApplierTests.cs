using System.Collections.Generic;
using System.Linq;
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

        Assert.Equal(new[] { new MechanicColumn("slam", "Slam", "Slam hits"), new MechanicColumn("tear", "Tear", "Tears", false) },
            model.MechanicColumns);
        TestFiles.AssertCounts(model.Players[0].CombinedMechanics, ("slam", 4), ("tear", 0)); // 5 + 1 - 2
        TestFiles.AssertCounts(model.Players[1].CombinedMechanics, ("slam", 4), ("tear", 3));
    }

    [Fact]
    public void Apply_KnownEncounter_CountsEveryMechanicPerPlayerEvenWithZero()
    {
        var (model, log) = Create(100, ("Slam", "Alice", 2), ("Other", "Bob", 1));

        MechanicsApplier.Apply(model, log, Catalog);

        TestFiles.AssertCounts(model.Players[0].Mechanics, ("Slam", 2), ("Other", 0));
        TestFiles.AssertCounts(model.Players[1].Mechanics, ("Slam", 0), ("Other", 1));
    }

    [Fact]
    public void Apply_KnownEncounterWithoutAnyEvents_StillHasTheColumns()
    {
        var (model, log) = Create(100);

        MechanicsApplier.Apply(model, log, Catalog);

        Assert.Equal(new[] { "slam", "tear" }, model.MechanicColumns.Select(c => c.Key));
        Assert.All(model.Players, p => Assert.Equal(0, p.CombinedMechanics["slam"]));
    }

    [Fact]
    public void Apply_SameMechanicNameTwice_IsAddedUp()
    {
        var (model, log) = Create(100, ("Slam", "Alice", 1));
        log.Mechanics.Add(new EiMechanic { Name = "Slam", MechanicsData = { new EiMechanicEvent { Actor = "Alice" } } });

        MechanicsApplier.Apply(model, log, Catalog);

        Assert.Equal(2, model.Players[0].Mechanics["Slam"]);
    }

    [Fact]
    public void Apply_UnknownEncounter_ShowsTheThreeMechanicsPlayersTriggeredMost()
    {
        var (model, log) = Create(999, ("Floor", "Alice", 4), ("Floor", "Bob", 3), ("Orb", "Alice", 5),
            ("Beam", "Bob", 2), ("Cone", "Alice", 1));

        MechanicsApplier.Apply(model, log, Catalog);

        Assert.Equal(new[] { "Floor", "Orb", "Beam" }, model.MechanicColumns.Select(c => c.Label));
        Assert.Equal(new MechanicColumn("gen_Floor", "Floor", "Floor description"), model.MechanicColumns[0]);
        TestFiles.AssertCounts(model.Players[0].CombinedMechanics, ("gen_Floor", 4), ("gen_Orb", 5));
        TestFiles.AssertCounts(model.Players[1].CombinedMechanics, ("gen_Floor", 3), ("gen_Beam", 2));
    }

    [Fact]
    public void Apply_UnknownEncounter_SkipsMechanicsEveryEncounterHasAndEnemyEvents()
    {
        var (model, log) = Create(999, ("Res", "Alice", 9), ("Boss attack", "Training Golem", 20), ("Floor", "Alice", 1));

        MechanicsApplier.Apply(model, log, Catalog);

        Assert.Equal(new[] { "Floor" }, model.MechanicColumns.Select(c => c.Label));
    }

    [Fact]
    public void Apply_UnknownEncounterWithTiedMechanics_SortsByName()
    {
        var (model, log) = Create(999, ("Zeta", "Alice", 2), ("Alpha", "Alice", 2), ("Mid", "Alice", 2), ("Beta", "Alice", 2));

        MechanicsApplier.Apply(model, log, Catalog);

        Assert.Equal(new[] { "Alpha", "Beta", "Mid" }, model.MechanicColumns.Select(c => c.Label));
    }

    [Fact]
    public void Apply_UnknownEncounterWithoutPlayerMechanics_HasNoColumns()
    {
        var (model, log) = Create(999, ("Res", "Alice", 3), ("Boss attack", "Training Golem", 20));

        MechanicsApplier.Apply(model, log, Catalog);

        Assert.Empty(model.MechanicColumns);
        Assert.All(model.Players, p => Assert.Empty(p.CombinedMechanics));
    }

    [Fact]
    public void Apply_UnknownMechanicWithoutDescription_UsesTheFullNameAsTitle()
    {
        var (model, log) = Create(999, ("Floor", "Alice", 1));
        log.Mechanics[0].Description = "";

        MechanicsApplier.Apply(model, log, Catalog);

        Assert.Equal("Floor (full)", model.MechanicColumns.Single().Title);
    }
}
