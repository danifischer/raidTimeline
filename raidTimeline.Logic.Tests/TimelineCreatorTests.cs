using System.IO;
using System.Linq;
using System.Threading;
using FluentAssertions;
using Xunit;

namespace raidTimeline.Logic.Tests;

public class TimelineCreatorTests
{
    private readonly TimelineCreator _subjectUnderTest = new(null);

    private static void CopyFixture(string directory, string name = "log.json") =>
        File.Copy(TestFiles.ValeGuardianLog, Path.Combine(directory, name));

    [Fact]
    public void ParseFilesFromDisk_FolderWithLogs_ReturnsOneModelPerParsableFile()
    {
        using var temp = TestFiles.CreateTempDirectory();
        CopyFixture(temp.Path, "a.json");
        CopyFixture(temp.Path, "b.json");
        File.WriteAllText(Path.Combine(temp.Path, "not_a_log.json"), "{}");

        var models = _subjectUnderTest.ParseFilesFromDisk(temp.Path, CancellationToken.None);

        models.Should().HaveCount(2);
        models.Select(m => m.EncounterName).Should().OnlyContain(n => n == "Vale Guardian");
    }

    [Fact]
    public void ParseFilesFromDisk_ManyFiles_KeepsEveryModel()
    {
        // Files are parsed in parallel; a non thread-safe result list loses models or throws.
        using var temp = TestFiles.CreateTempDirectory();
        for (var i = 0; i < 40; i++)
        {
            CopyFixture(temp.Path, $"log{i}.json");
        }

        var models = _subjectUnderTest.ParseFilesFromDisk(temp.Path, CancellationToken.None);

        models.Should().HaveCount(40);
        models.Select(m => m.LogPath).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void ParseFilesFromDiskWhileWatching_SkipsKnownFiles()
    {
        using var temp = TestFiles.CreateTempDirectory();
        CopyFixture(temp.Path, "known.json");
        CopyFixture(temp.Path, "new.json");
        var known = _subjectUnderTest.ParseFilesFromDisk(temp.Path, CancellationToken.None)
            .Where(m => m.LogUrl == "known.html").ToList();

        var models = _subjectUnderTest.ParseFilesFromDiskWhileWatching(temp.Path, "timeline.html", known);

        models.Select(m => m.LogUrl).Should().BeEquivalentTo("known.html", "new.html");
    }

    [Fact]
    public void BuildTimelineFile_WithModel_WritesTimelineAndStatisticsFiles()
    {
        using var temp = TestFiles.CreateTempDirectory();
        CopyFixture(temp.Path);
        var models = _subjectUnderTest.ParseFilesFromDisk(temp.Path, CancellationToken.None);

        _subjectUnderTest.BuildTimelineFile(temp.Path, "timeline.html", models);

        var timeline = File.ReadAllText(Path.Combine(temp.Path, "timeline.html"));
        timeline.Should().Contain("Vale Guardian").And.NotContain("{{placeholder}}");
        File.ReadAllText(Path.Combine(temp.Path, "statistics.html")).Should().Contain("Player1.1001")
            .And.NotContain("{{placeholder}}");
    }

    [Fact]
    public void BuildTimelineFile_ExistingOutput_IsReplaced()
    {
        using var temp = TestFiles.CreateTempDirectory();
        CopyFixture(temp.Path);
        var models = _subjectUnderTest.ParseFilesFromDisk(temp.Path, CancellationToken.None);
        File.WriteAllText(Path.Combine(temp.Path, "timeline.html"), "OLD CONTENT");

        _subjectUnderTest.BuildTimelineFile(temp.Path, "timeline.html", models);

        File.ReadAllText(Path.Combine(temp.Path, "timeline.html")).Should().NotContain("OLD CONTENT");
    }
}
