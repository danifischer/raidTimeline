using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json;
using raidTimeline.Logic.DpsReport;
using Xunit;

namespace raidTimeline.Logic.Tests;

public class LogDownloaderTests
{
    private const string Token = "test-token";
    private const string BaseUrl = "https://dps.report/";

    private static readonly DateTimeOffset LogTime = new(2021, 7, 15, 20, 14, 12, TimeSpan.FromHours(2));

    // The downloader compares against the local day of the upload timestamp.
    private static string Day(DateTimeOffset time) => time.ToLocalTime().ToString("yyyyMMdd");

    [Fact]
    public async Task DownloadLogsForOneDay_MatchingUploads_ReturnsParsedModelsWithPermalink()
    {
        using var temp = TestFiles.CreateTempDirectory();
        var handler = new StubHandler()
            .WithUploads(Token, 1, 1, ("one", LogTime), ("two", LogTime))
            .WithLog("one").WithLog("two");

        var models = await Create(handler).DownloadLogsForOneDayAsync(temp.Path, Token, Day(LogTime), default);

        models.Select(m => m.LogUrl).Should().Equal(BaseUrl + "one", BaseUrl + "two");
        models.Should().OnlyContain(m => m.EncounterName == "Soulless Horror" && m.Players.Count == 10);
        Directory.GetFiles(temp.Path).Should().BeEmpty("temporary html files must be cleaned up");
    }

    [Fact]
    public async Task DownloadLogsForOneDay_FiltersUploadsOfOtherDays()
    {
        using var temp = TestFiles.CreateTempDirectory();
        var handler = new StubHandler()
            .WithUploads(Token, 1, 1, ("today", LogTime), ("other", LogTime.AddDays(-3)))
            .WithLog("today");

        var models = await Create(handler).DownloadLogsForOneDayAsync(temp.Path, Token, Day(LogTime), default);

        models.Select(m => m.LogUrl).Should().Equal(BaseUrl + "today");
        handler.Requests.Should().NotContain(BaseUrl + "other");
    }

    [Fact]
    public async Task DownloadLogsForOneDay_MultiplePages_RequestsAllPages()
    {
        using var temp = TestFiles.CreateTempDirectory();
        var handler = new StubHandler()
            .WithUploads(Token, 1, 2, ("p1", LogTime))
            .WithUploads(Token, 2, 2, ("p2", LogTime))
            .WithLog("p1").WithLog("p2");

        var models = await Create(handler).DownloadLogsForOneDayAsync(temp.Path, Token, Day(LogTime), default);

        models.Should().HaveCount(2);
        handler.Requests.Count(r => r.Contains("getUploads")).Should().Be(2);
    }

    [Fact]
    public async Task DownloadLogsForOneDay_UnparsableLog_IsSkipped()
    {
        using var temp = TestFiles.CreateTempDirectory();
        var handler = new StubHandler()
            .WithUploads(Token, 1, 1, ("bad", LogTime), ("good", LogTime))
            .WithHtml("bad", "<html>no log data</html>").WithLog("good");

        var models = await Create(handler).DownloadLogsForOneDayAsync(temp.Path, Token, Day(LogTime), default);

        models.Select(m => m.LogUrl).Should().Equal(BaseUrl + "good");
        Directory.GetFiles(temp.Path).Should().BeEmpty();
    }

    [Fact]
    public async Task DownloadLogsForOneDay_UserTokenIsUrlEncoded()
    {
        using var temp = TestFiles.CreateTempDirectory();
        var handler = new StubHandler().WithUploads("a&b=c", 1, 1);

        await Create(handler).DownloadLogsForOneDayAsync(temp.Path, "a&b=c", Day(LogTime), default);

        handler.Requests.Single().Should().Contain("userToken=a%26b%3Dc");
    }

    [Fact]
    public async Task DownloadLogsForOneDay_AlreadyCancelled_ReturnsNothingWithoutRequests()
    {
        using var temp = TestFiles.CreateTempDirectory();
        var handler = new StubHandler().WithUploads(Token, 1, 1, ("one", LogTime)).WithLog("one");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var models = await Create(handler).DownloadLogsForOneDayAsync(temp.Path, Token, Day(LogTime), cts.Token);

        models.Should().BeEmpty();
        handler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task DownloadLogsForOneDay_ServerError_Throws()
    {
        using var temp = TestFiles.CreateTempDirectory();
        var handler = new StubHandler();

        var act = () => Create(handler).DownloadLogsForOneDayAsync(temp.Path, Token, Day(LogTime), default);

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    private static LogDownloader Create(StubHandler handler) => new(new HttpClient(handler));

    /// <summary>Stands in for dps.report so tests never touch the network. Unknown urls return 404.</summary>
    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Dictionary<string, string> _responses = new();

        internal List<string> Requests { get; } = new();

        internal StubHandler WithUploads(string token, int page, int pages,
            params (string id, DateTimeOffset time)[] uploads)
        {
            var body = JsonConvert.SerializeObject(new
            {
                pages,
                uploads = uploads.Select(u => new
                {
                    permalink = BaseUrl + u.id,
                    encounterTime = u.time.ToUnixTimeSeconds()
                })
            });
            _responses[$"{BaseUrl}getUploads?userToken={Uri.EscapeDataString(token)}&page={page}"] = body;
            return this;
        }

        internal StubHandler WithLog(string id) => WithHtml(id, File.ReadAllText(TestFiles.ParsedLog));

        internal StubHandler WithHtml(string id, string html)
        {
            _responses[BaseUrl + id] = html;
            return this;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var url = request.RequestUri!.AbsoluteUri;
            Requests.Add(url);

            return Task.FromResult(_responses.TryGetValue(url, out var body)
                ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) }
                : new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }
}
