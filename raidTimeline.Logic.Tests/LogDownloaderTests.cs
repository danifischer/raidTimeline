using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
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
        var handler = new StubHandler()
            .WithUploads(Token, 1, 1, ("one", LogTime), ("two", LogTime))
            .WithLog("one").WithLog("two");

        var models = await Create(handler).DownloadLogsForOneDayAsync(Token, Day(LogTime), default);

        Assert.Equal(new[] { BaseUrl + "one", BaseUrl + "two" }, models.Select(m => m.LogUrl));
        Assert.All(models, m =>
        {
            Assert.Equal("Vale Guardian", m.EncounterName);
            Assert.Equal(10, m.Players.Count);
        });
    }

    [Fact]
    public async Task DownloadLogsForOneDay_FiltersUploadsOfOtherDays()
    {
        var handler = new StubHandler()
            .WithUploads(Token, 1, 1, ("today", LogTime), ("other", LogTime.AddDays(-3)))
            .WithLog("today");

        var models = await Create(handler).DownloadLogsForOneDayAsync(Token, Day(LogTime), default);

        Assert.Equal(new[] { BaseUrl + "today" }, models.Select(m => m.LogUrl));
        Assert.DoesNotContain(handler.Requests, r => r.Contains("other"));
    }

    [Fact]
    public async Task DownloadLogsForOneDay_MultiplePages_RequestsAllPages()
    {
        var handler = new StubHandler()
            .WithUploads(Token, 1, 2, ("p1", LogTime))
            .WithUploads(Token, 2, 2, ("p2", LogTime))
            .WithLog("p1").WithLog("p2");

        var models = await Create(handler).DownloadLogsForOneDayAsync(Token, Day(LogTime), default);

        Assert.Equal(2, models.Count);
        Assert.Equal(2, handler.Requests.Count(r => r.Contains("getUploads")));
    }

    [Fact]
    public async Task DownloadLogsForOneDay_UnparsableLog_IsSkipped()
    {
        var handler = new StubHandler()
            .WithUploads(Token, 1, 1, ("bad", LogTime), ("good", LogTime))
            .WithJson("bad", "{}").WithLog("good");

        var models = await Create(handler).DownloadLogsForOneDayAsync(Token, Day(LogTime), default);

        Assert.Equal(new[] { BaseUrl + "good" }, models.Select(m => m.LogUrl));
    }

    [Fact]
    public async Task DownloadLogsForOneDay_UserTokenIsUrlEncoded()
    {
        var handler = new StubHandler().WithUploads("a&b=c", 1, 1);

        await Create(handler).DownloadLogsForOneDayAsync("a&b=c", Day(LogTime), default);

        Assert.Contains("userToken=a%26b%3Dc", Assert.Single(handler.Requests));
    }

    [Fact]
    public async Task DownloadLogsForOneDay_AlreadyCancelled_ReturnsNothingWithoutRequests()
    {
        var handler = new StubHandler().WithUploads(Token, 1, 1, ("one", LogTime)).WithLog("one");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var models = await Create(handler).DownloadLogsForOneDayAsync(Token, Day(LogTime), cts.Token);

        Assert.Empty(models);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task DownloadLogsForOneDay_ServerError_Throws()
    {
        var handler = new StubHandler();

        await Assert.ThrowsAsync<HttpRequestException>(
            () => Create(handler).DownloadLogsForOneDayAsync(Token, Day(LogTime), default));
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

        internal StubHandler WithLog(string id) => WithJson(id, File.ReadAllText(TestFiles.ValeGuardianLog));

        internal StubHandler WithJson(string id, string json)
        {
            _responses[$"{BaseUrl}getJson?permalink={Uri.EscapeDataString(BaseUrl + id)}"] = json;
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
