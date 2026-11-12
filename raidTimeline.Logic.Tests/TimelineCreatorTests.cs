using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;
using raidTimeline.Models.Models;
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

        Assert.Equal(2, models.Count);
        Assert.All(models, m => Assert.Equal("Vale Guardian", m.EncounterName));
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

        Assert.Equal(40, models.Count);
        Assert.Equal(40, models.Select(m => m.LogPath).Distinct().Count());
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

        Assert.Equal(new[] { "known.html", "new.html" }, models.Select(m => m.LogUrl).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void BuildTimelineFile_WithModel_WritesOneFileWithTheRaidData()
    {
        using var temp = TestFiles.CreateTempDirectory();
        CopyFixture(temp.Path);
        var models = _subjectUnderTest.ParseFilesFromDisk(temp.Path, CancellationToken.None);

        _subjectUnderTest.BuildTimelineFile(temp.Path, "timeline.html", models);

        var timeline = File.ReadAllText(Path.Combine(temp.Path, "timeline.html"));
        Assert.Contains("<script type=\"application/json\" id=\"raid-data\">", timeline);
        Assert.Contains("Vale Guardian", timeline);
        Assert.Contains("Player1.1001", timeline);
        Assert.DoesNotContain("{{data}}", timeline);
    }

    [Fact]
    public void BuildTimelineFile_LeavesOnlyTheTimeline()
    {
        // The statistics are part of the timeline page, and no temporary file may stay behind.
        using var temp = TestFiles.CreateTempDirectory();
        CopyFixture(temp.Path);
        var models = _subjectUnderTest.ParseFilesFromDisk(temp.Path, CancellationToken.None);

        _subjectUnderTest.BuildTimelineFile(temp.Path, "timeline.html", models);

        Assert.Equal(new[] { "log.json", "timeline.html" },
            Directory.GetFiles(temp.Path).Select(Path.GetFileName).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void BuildTimelineFile_WithoutModels_WritesAnEmptyPage()
    {
        using var temp = TestFiles.CreateTempDirectory();

        _subjectUnderTest.BuildTimelineFile(temp.Path, "timeline.html", Array.Empty<RaidModel>());

        Assert.Contains("\"logs\":[]", File.ReadAllText(Path.Combine(temp.Path, "timeline.html")));
    }

    [Fact]
    public void BuildTimelineFile_CancelledBeforeStart_WritesNothing()
    {
        using var temp = TestFiles.CreateTempDirectory();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        _subjectUnderTest.BuildTimelineFile(temp.Path, "timeline.html", Array.Empty<RaidModel>(),
            cancellationToken: cancellation.Token);

        Assert.False(File.Exists(Path.Combine(temp.Path, "timeline.html")));
    }

    [Fact]
    public void BuildTimelineFile_SeveralTimelinesWithTheSameNameAtOnce_DoNotInterfere()
    {
        // The files used to be written to the current directory first, where parallel runs overwrote each other.
        var directories = Enumerable.Range(0, 8).Select(_ => TestFiles.CreateTempDirectory()).ToList();
        try
        {
            foreach (var directory in directories) CopyFixture(directory.Path);
            var models = _subjectUnderTest.ParseFilesFromDisk(directories[0].Path, CancellationToken.None);

            Parallel.ForEach(directories,
                directory => _subjectUnderTest.BuildTimelineFile(directory.Path, "timeline.html", models));

            foreach (var directory in directories)
            {
                Assert.Contains("Vale Guardian", File.ReadAllText(Path.Combine(directory.Path, "timeline.html")));
            }
        }
        finally
        {
            foreach (var directory in directories) directory.Dispose();
        }
    }

    [Fact]
    public void BuildTimelineFile_ExistingOutput_IsReplaced()
    {
        using var temp = TestFiles.CreateTempDirectory();
        CopyFixture(temp.Path);
        var models = _subjectUnderTest.ParseFilesFromDisk(temp.Path, CancellationToken.None);
        File.WriteAllText(Path.Combine(temp.Path, "timeline.html"), "OLD CONTENT");

        _subjectUnderTest.BuildTimelineFile(temp.Path, "timeline.html", models);

        Assert.DoesNotContain("OLD CONTENT", File.ReadAllText(Path.Combine(temp.Path, "timeline.html")));
    }
}
