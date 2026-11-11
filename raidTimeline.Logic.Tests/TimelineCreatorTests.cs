using System.IO;
using System.Linq;
using System.Threading;
using FluentAssertions;
using Xunit;

namespace raidTimeline.Logic.Tests;

public class TimelineCreatorTests
{
    private readonly TimelineCreator _subjectUnderTest = new(null);

    private static void CopyFixture(string directory, string name = "log.html") =>
        File.Copy(TestFiles.ParsedLog, Path.Combine(directory, name));

    [Fact]
    public void ParseFilesFromDisk_FolderWithLogs_ReturnsOneModelPerParsableFile()
    {
        using var temp = TestFiles.CreateTempDirectory();
        CopyFixture(temp.Path, "a.html");
        CopyFixture(temp.Path, "b.html");
        File.WriteAllText(Path.Combine(temp.Path, "not_a_log.html"), "<html></html>");

        var models = _subjectUnderTest.ParseFilesFromDisk(temp.Path, CancellationToken.None);

        models.Should().HaveCount(2);
        models.Select(m => m.EncounterName).Should().OnlyContain(n => n == "Soulless Horror");
    }

    [Fact]
    public void ParseFilesFromDiskWhileWatching_SkipsKnownFilesAndOutputFile()
    {
        using var temp = TestFiles.CreateTempDirectory();
        CopyFixture(temp.Path, "known.html");
        CopyFixture(temp.Path, "timeline.html");
        CopyFixture(temp.Path, "new.html");
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
        timeline.Should().Contain("Soulless Horror").And.NotContain("{{placeholder}}");
        File.ReadAllText(Path.Combine(temp.Path, "statistics.html")).Should().Contain("Max Zero.2167")
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
