using System.Linq;
using raidTimeline.Logic.Mechanics;
using Xunit;

namespace raidTimeline.Logic.Tests;

public class CustomMechanicsTests
{
    private const string Column = """{ "key": "k", "label": "L", "title": "T", "mechanics": ["m"] }""";

    private static MechanicsCatalog Shipped(string encounters, string ignored = "") =>
        MechanicsCatalog.Parse($$"""{ "ignoredMechanics": [{{ignored}}], "encounters": [{{encounters}}] }""");

    private static string Encounter(string name, string ids) =>
        $$"""{ "name": "{{name}}", "ids": [{{ids}}], "columns": [{{Column}}] }""";

    [Fact]
    public void WithOverrides_NewEncounter_IsAddedToTheShippedOnes()
    {
        var merged = Shipped(Encounter("Old", "1")).WithOverrides(Shipped(Encounter("New", "2")));

        Assert.Equal("Old", merged.Find(1)!.Name);
        Assert.Equal("New", merged.Find(2)!.Name);
    }

    [Fact]
    public void WithOverrides_SameId_ReplacesTheShippedEncounter()
    {
        var merged = Shipped(Encounter("Old", "1") + "," + Encounter("Other", "3"))
            .WithOverrides(Shipped(Encounter("Mine", "1")));

        Assert.Equal("Mine", merged.Find(1)!.Name);
        Assert.Equal(new[] { "Other", "Mine" }, merged.Encounters.Select(e => e.Name));
    }

    [Fact]
    public void WithOverrides_OneOfSeveralIds_KeepsTheOthersOfTheShippedEncounter()
    {
        var shipped = Shipped(Encounter("Trio", "1, 2, 3"));
        var merged = shipped.WithOverrides(Shipped(Encounter("Mine", "2")));

        Assert.Equal("Trio", merged.Find(1)!.Name);
        Assert.Equal("Mine", merged.Find(2)!.Name);
        Assert.Equal("Trio", merged.Find(3)!.Name);
        Assert.Equal(new[] { 1, 2, 3 }, shipped.Find(2)!.Ids); // the shipped catalog is not changed
    }

    [Fact]
    public void WithOverrides_IgnoredMechanics_AreAddedUp()
    {
        var merged = Shipped("", "\"Res\", \"Dead\"").WithOverrides(Shipped("", "\"Dead\", \"Flt\""));

        Assert.True(new[] { "Res", "Dead", "Flt" }.ToHashSet().SetEquals(merged.IgnoredMechanics));
    }

    [Fact]
    public void Template_IsAValidFileThatChangesNothing()
    {
        var shipped = MechanicsCatalog.Shipped;

        var merged = shipped.WithOverrides(MechanicsCatalog.Parse(CustomMechanics.Template));

        Assert.Equal(shipped.Encounters.Select(e => e.Name), merged.Encounters.Select(e => e.Name));
        Assert.Equal(shipped.IgnoredMechanics.Count, merged.IgnoredMechanics.Count);
    }

    [Fact]
    public void Load_MissingFile_ChangesNothing()
    {
        Assert.False(CustomMechanics.Load(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "does-not-exist-mechanics.json")));
        Assert.Same(MechanicsCatalog.Shipped, MechanicsCatalog.Default);
    }
}
