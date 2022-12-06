using System;
using System.IO;
using System.Threading;
using FluentAssertions;
using Xunit;

namespace raidTimeline.Logic.Tests;

public class TimelineCreatorTests : IDisposable
{
    private readonly string _path;

    public TimelineCreatorTests()
    {
        _path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_path);
    }

    public void Dispose()
    {
        Directory.Delete(_path);
    }
    
    [Fact]
    public void ParseFileFromWeb_ValidInput_ProvidesCorrectModelData()
    {
        var subjectUnderTest = new TimelineCreator();

        var models = subjectUnderTest.ParseFileFromWeb(_path, 
            "qla4m7fb2qc1dm2askitfmjimvtoltjb", 
            "20220221",
            CancellationToken.None);

        models.Count.Should().Be(1);
    }
}