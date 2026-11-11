using System.IO;
using System.Linq;
using FluentAssertions;
using raidTimeline.Logic.Mechanics;
using Xunit;

namespace raidTimeline.Logic.Tests;

public class MechanicsCatalogTests
{
    // All raid encounters which have a mechanics table, see mechanics.json
    private static readonly int[] RaidIds =
    {
        15438, 15429, 15375, 16123, 16088, 16137, 16125, 16115, 16235, 16246, 17194, 17172, 17188, 17154, 19767, 19828,
        19691, 19536, 19844, 19651, 19450, 43974, 44885, 21105, 21089, 20934, 22006, 21964, 22000
    };

    [Fact]
    public void Default_ContainsAllRaidEncounters()
    {
        MechanicsCatalog.Default.Encounters.Should().HaveCount(24);
        foreach (var id in RaidIds)
        {
            MechanicsCatalog.Default.Find(id).Should().NotBeNull("encounter {0} needs a mechanics table", id);
        }
    }

    [Fact]
    public void Default_EveryEncounterHasLabelsTitlesAndMechanics()
    {
        foreach (var encounter in MechanicsCatalog.Default.Encounters)
        {
            encounter.Name.Should().NotBeNullOrWhiteSpace();
            encounter.Columns.Should().NotBeEmpty();
            encounter.Columns.Should().OnlyContain(c =>
                c.Label != "" && c.Title != "" && c.Mechanics.Count > 0, encounter.Name);
        }
    }

    [Fact]
    public void Default_IgnoresTheMechanicsEveryEncounterHas()
    {
        MechanicsCatalog.Default.IgnoredMechanics.Should()
            .Contain(new[] { "Miss.Qck", "Miss.Alac", "Downed", "Got up", "Res", "Dead" });
    }

    [Fact]
    public void Default_StabilizedHitsAreSubtractedOnlyWhereTheyAreAPartOfTheOtherMechanic()
    {
        var column = MechanicsCatalog.Default.Find(43974)!.Columns.Single(c => c.Key == "ca_arm");

        column.Mechanics.Should().Equal("Arm Slam");
        column.Subtract.Should().Equal("Stab.Slam");
    }

    [Fact]
    public void Default_ColumnsWhichAreNoFailsAreNotRanked()
    {
        MechanicsCatalog.Default.Find(17154)!.Columns.Where(c => !c.Rank).Select(c => c.Key).Should().Equal("dei_tear");
        MechanicsCatalog.Default.Find(19691)!.Columns.Where(c => c.Rank).Select(c => c.Key).Should()
            .Equal("brokenking_cracks");
        MechanicsCatalog.Default.Find(19844)!.Columns.Where(c => c.Rank).Select(c => c.Key).Should()
            .Equal("eyes_beam");
    }

    [Fact]
    public void Parse_ValidDefinition_ReadsComments()
    {
        const string json = """
            {
              // a comment
              "ignoredMechanics": ["Res"],
              "encounters": [
                { "name": "Test", "ids": [1, 2], "columns": [
                    { "key": "a", "label": "A", "title": "Column A", "mechanics": ["x", "y"], },
                ] }
              ]
            }
            """;

        var catalog = MechanicsCatalog.Parse(json);

        catalog.Find(2)!.Name.Should().Be("Test");
        catalog.Find(1)!.Columns.Single().Mechanics.Should().Equal("x", "y");
        catalog.IgnoredMechanics.Should().Equal("Res");
        catalog.Find(3).Should().BeNull();
    }

    [Theory]
    [InlineData("not json", "not valid json")]
    [InlineData("null", "empty")]
    [InlineData("""{"encounters":[{"name":"A","ids":[],"columns":[{"key":"k","label":"L","mechanics":["m"]}]}]}""", "no ids")]
    [InlineData("""{"encounters":[{"name":"A","ids":[1],"columns":[]}]}""", "no columns")]
    [InlineData("""{"encounters":[{"name":"A","ids":[1],"columns":[{"key":"","label":"L","mechanics":["m"]}]}]}""", "empty or duplicate column key")]
    [InlineData("""{"encounters":[{"name":"A","ids":[1],"columns":[{"key":"k","label":"L","mechanics":["m"]},{"key":"k","label":"L","mechanics":["m"]}]}]}""", "duplicate column key")]
    [InlineData("""{"encounters":[{"name":"A","ids":[1],"columns":[{"key":"k","label":"","mechanics":["m"]}]}]}""", "no label")]
    [InlineData("""{"encounters":[{"name":"A","ids":[1],"columns":[{"key":"k","label":"L","mechanics":[]}]}]}""", "no mechanics")]
    [InlineData("""{"encounters":[{"name":"A","ids":[1],"columns":[{"key":"k","label":"L","mechanics":["m"],"rank":false}]}]}""", "ranking")]
    [InlineData("""{"encounters":[{"name":"A","ids":[1],"columns":[{"key":"k","label":"L","mechanics":["m"]}]},{"name":"B","ids":[1],"columns":[{"key":"k","label":"L","mechanics":["m"]}]}]}""", "Encounter id 1 is used by 'A' and 'B'")]
    public void Parse_InvalidDefinition_ThrowsWithAReadableMessage(string json, string expectedMessage)
    {
        var act = () => MechanicsCatalog.Parse(json);

        act.Should().Throw<InvalidDataException>().WithMessage($"*{expectedMessage}*");
    }
}
