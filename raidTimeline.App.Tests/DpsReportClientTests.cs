using System.Net;
using System.Text;
using FluentAssertions;
using raidTimeline.App.Services;
using Xunit;

namespace raidTimeline.App.Tests;

public sealed class DpsReportClientTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "rta-dps-tests-" + Guid.NewGuid().ToString("N"));
    private readonly string _log;

    public DpsReportClientTests()
    {
        Directory.CreateDirectory(_folder);
        _log = Path.Combine(_folder, "20261111-201500.zevtc");
        File.WriteAllBytes(_log, new byte[] { 1, 2, 3, 4, 5 });
    }

    public void Dispose()
    {
        if (Directory.Exists(_folder)) Directory.Delete(_folder, true);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) => _respond = respond;

        public Uri? RequestUri { get; private set; }
        public string? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            RequestUri = request.RequestUri;
            RequestBody = request.Content == null
                ? null
                : Encoding.Latin1.GetString(await request.Content.ReadAsByteArrayAsync(ct));
            return _respond(request);
        }
    }

    private static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private (DpsReportClient Client, StubHandler Handler) Create(Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        var handler = new StubHandler(respond);
        return (new DpsReportClient(new HttpClient(handler)), handler);
    }

    [Fact]
    public async Task UploadAsync_Success_ReturnsLinkKillAndBossId()
    {
        var (client, _) = Create(_ => Json("""
            { "id": "abcd-2026", "permalink": "https://dps.report/abcd-20261111-201500_vg",
              "encounter": { "success": true, "bossId": 15438 }, "evtc": { "bossId": 15438, "type": 1 } }
            """));

        var upload = await client.UploadAsync(_log, "token", CancellationToken.None);

        upload.Should().Be(new DpsReportUpload("https://dps.report/abcd-20261111-201500_vg", true, 15438));
    }

    [Fact]
    public async Task UploadAsync_FailedAttempt_IsNotASuccess()
    {
        var (client, _) = Create(_ => Json("""
            { "permalink": "https://dps.report/x", "encounter": { "success": false }, "evtc": { "bossId": 17188 } }
            """));

        var upload = await client.UploadAsync(_log, "token", CancellationToken.None);

        upload.Success.Should().BeFalse();
        upload.BossId.Should().Be(17188);
    }

    [Fact]
    public async Task UploadAsync_WithoutEncounterData_StillReturnsTheLink()
    {
        var (client, _) = Create(_ => Json("""{ "permalink": "https://dps.report/x" }"""));

        var upload = await client.UploadAsync(_log, "token", CancellationToken.None);

        upload.Should().Be(new DpsReportUpload("https://dps.report/x", false, 0));
    }

    [Fact]
    public async Task UploadAsync_SendsTheLogAsMultipartAndEncodesTheToken()
    {
        var (client, handler) = Create(_ => Json("""{ "permalink": "https://dps.report/x" }"""));

        await client.UploadAsync(_log, "a&b=c d", CancellationToken.None);

        handler.RequestUri!.AbsoluteUri.Should().Be(
            "https://dps.report/uploadContent?userToken=a%26b%3Dc%20d&generator=ei&json=1");
        handler.RequestBody.Should().Contain("name=file").And.Contain("filename=20261111-201500.zevtc")
            .And.Contain("\u0001\u0002\u0003\u0004\u0005");
    }

    [Fact]
    public async Task UploadAsync_RateLimited_ThrowsWithTheMessageOfDpsReport()
    {
        var (client, _) = Create(_ => Json(
            """{ "error": "Rate limit exceeded. Try again in 60 seconds.", "rateLimited": true }""",
            HttpStatusCode.TooManyRequests));

        var act = () => client.UploadAsync(_log, "token", CancellationToken.None);

        var exception = (await act.Should().ThrowAsync<DpsReportException>()).Which;
        exception.Message.Should().Contain("Rate limit exceeded");
        exception.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task UploadAsync_ErrorInAnOkResponse_Throws()
    {
        var (client, _) = Create(_ => Json("""{ "error": "Unsupported log" }"""));

        var act = () => client.UploadAsync(_log, "token", CancellationToken.None);

        (await act.Should().ThrowAsync<DpsReportException>()).Which.Message.Should().Be("Unsupported log");
    }

    [Fact]
    public async Task UploadAsync_HtmlErrorPage_ThrowsWithTheStatus()
    {
        var (client, _) = Create(_ => new HttpResponseMessage(HttpStatusCode.BadGateway)
            { Content = new StringContent("<html>Bad gateway</html>") });

        var act = () => client.UploadAsync(_log, "token", CancellationToken.None);

        (await act.Should().ThrowAsync<DpsReportException>()).Which.Message.Should().Contain("502");
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{ "permalink": "" }""")]
    [InlineData("[]")]
    public async Task UploadAsync_ResponseWithoutReport_Throws(string json)
    {
        var (client, _) = Create(_ => Json(json));

        var act = () => client.UploadAsync(_log, "token", CancellationToken.None);

        await act.Should().ThrowAsync<DpsReportException>();
    }

    [Fact]
    public async Task UploadAsync_MissingLogFile_Throws()
    {
        var (client, handler) = Create(_ => Json("""{ "permalink": "https://dps.report/x" }"""));

        var act = () => client.UploadAsync(Path.Combine(_folder, "missing.zevtc"), "token", CancellationToken.None);

        await act.Should().ThrowAsync<IOException>();
        handler.RequestUri.Should().BeNull("nothing is sent without a file");
    }

    [Fact]
    public async Task UploadAsync_ServerUnreachable_ThrowsHttpRequestException()
    {
        var (client, _) = Create(_ => throw new HttpRequestException("no connection"));

        var act = () => client.UploadAsync(_log, "token", CancellationToken.None);

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Theory]
    [InlineData(true, true, true, "0x3C4E,0x3C45", 15438, true)] // kill of a filtered boss
    [InlineData(true, true, true, "0x3C4E,0x3C45", 17188, false)] // boss is not in the filter
    [InlineData(true, false, false, "0x3C4E", 15438, false)] // wipe, only kills wanted
    [InlineData(false, false, false, "0x3C4E", 17188, true)] // everything wanted
    [InlineData(false, true, false, "", 15438, true)] // no filter requested, so an empty filter is fine
    [InlineData(false, true, true, "", 15438, false)] // filter requested but there is none
    [InlineData(false, true, true, null, 15438, false)]
    [InlineData(false, true, true, "0x3c4e", 15438, true)] // lower case hex works as well
    public void IsWanted_KillOnlyAndBossFilter(bool killOnly, bool success, bool filter, string? bossFilter,
        int bossId, bool expected)
    {
        var upload = new DpsReportUpload("https://dps.report/x", success, bossId);

        DpsReportUploadService.IsWanted(upload, killOnly, filter, bossFilter).Should().Be(expected);
    }
}
