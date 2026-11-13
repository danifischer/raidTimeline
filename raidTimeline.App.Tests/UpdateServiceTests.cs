using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using raidTimeline.App.Services;
using Xunit;

namespace raidTimeline.App.Tests;

public sealed class UpdateServiceTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "rta-update-tests-" + Guid.NewGuid().ToString("N"));

    public UpdateServiceTests()
    {
        Directory.CreateDirectory(_folder);
    }

    public void Dispose()
    {
        if (Directory.Exists(_folder)) Directory.Delete(_folder, true);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<Uri, HttpResponseMessage> _respond;

        public StubHandler(Func<Uri, HttpResponseMessage> respond) => _respond = respond;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            Task.FromResult(_respond(request.RequestUri!));
    }

    private static SemVer V(string text)
    {
        Assert.True(SemVer.TryParse(text, out var version));
        return version;
    }

    [Theory]
    [InlineData("3.1.0", "3.0.0", 1)]
    [InlineData("v3.0.1", "3.0.0", 1)]
    [InlineData("3.0.0", "3.0.0+abc123", 0)]
    [InlineData("3.0.0", "3.0.0-beta.1", 1)]
    [InlineData("3.0.0-beta.2", "3.0.0-beta.10", -1)]
    [InlineData("3.0.0-alpha", "3.0.0-beta", -1)]
    [InlineData("3.0.0-1", "3.0.0-alpha", -1)]
    [InlineData("3.0.0-beta", "3.0.0-beta.1", -1)]
    [InlineData("2.9.9", "3.0.0", -1)]
    public void SemVer_Compares(string left, string right, int expected)
    {
        Assert.Equal(expected, Math.Sign(V(left).CompareTo(V(right))));
    }

    [Theory]
    [InlineData("")]
    [InlineData("latest")]
    [InlineData("3.0")]
    public void SemVer_RejectsNonVersions(string text)
    {
        Assert.False(SemVer.TryParse(text, out _));
    }

    [Fact]
    public async Task GetLatestReleaseAsync_ReadsTagAndAssets()
    {
        var service = new UpdateService(new HttpClient(new StubHandler(_ => Json("""
            { "tag_name": "v3.1.0", "assets": [
              { "name": "SHA256SUMS.txt", "browser_download_url": "https://example.test/sums" },
              { "name": "rta.3.1.0.nupkg", "browser_download_url": "https://example.test/rta" } ] }
            """))));

        var release = await service.GetLatestReleaseAsync(CancellationToken.None);

        Assert.Equal("3.1.0", release.Version.ToString());
        Assert.Equal("https://example.test/rta", release.Find("RTA.3.1.0.nupkg")?.Url);
    }

    [Fact]
    public async Task GetLatestReleaseAsync_HttpError_Throws()
    {
        var service = new UpdateService(new HttpClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Forbidden))));

        await Assert.ThrowsAsync<UpdateException>(() => service.GetLatestReleaseAsync(CancellationToken.None));
    }

    [Fact]
    public async Task GetLatestReleaseAsync_NotARelease_Throws()
    {
        var service = new UpdateService(new HttpClient(new StubHandler(_ => Json("""{ "message": "Not Found" }"""))));

        await Assert.ThrowsAsync<UpdateException>(() => service.GetLatestReleaseAsync(CancellationToken.None));
    }

    [Fact]
    public async Task DownloadVerifiedAsync_MatchingChecksum_ReturnsFile()
    {
        var content = Encoding.UTF8.GetBytes("zip content");
        var release = Release();
        var service = ServiceFor(content, Hash(content) + "  rta-v3.1.0-win-x64.zip\n");

        var path = await service.DownloadVerifiedAsync(release, "rta-v3.1.0-win-x64.zip", _folder, CancellationToken.None);

        Assert.Equal(content, await File.ReadAllBytesAsync(path));
    }

    [Fact]
    public async Task DownloadVerifiedAsync_WrongChecksum_Throws()
    {
        var service = ServiceFor(new byte[] { 1, 2, 3 }, new string('0', 64) + "  rta-v3.1.0-win-x64.zip\n");

        await Assert.ThrowsAsync<UpdateException>(() =>
            service.DownloadVerifiedAsync(Release(), "rta-v3.1.0-win-x64.zip", _folder, CancellationToken.None));
    }

    [Fact]
    public async Task DownloadVerifiedAsync_NoEntryInChecksums_Throws()
    {
        var service = ServiceFor(new byte[] { 1 }, new string('0', 64) + "  something-else.zip\n");

        await Assert.ThrowsAsync<UpdateException>(() =>
            service.DownloadVerifiedAsync(Release(), "rta-v3.1.0-win-x64.zip", _folder, CancellationToken.None));
    }

    [Fact]
    public void InstallZip_ReplacesFilesAndKeepsTheOldOnesUntilCleanup()
    {
        var target = Path.Combine(_folder, "app");
        Directory.CreateDirectory(target);
        File.WriteAllText(Path.Combine(target, "rta.exe"), "old exe");
        var zip = CreateZip(("rta.exe", "new exe"), ("config.json", "{}"));

        UpdateService.InstallZip(zip, target);

        Assert.Equal("new exe", File.ReadAllText(Path.Combine(target, "rta.exe")));
        Assert.Equal("{}", File.ReadAllText(Path.Combine(target, "config.json")));
        Assert.Equal("old exe", File.ReadAllText(Path.Combine(target, "rta.exe.old")));

        UpdateService.DeleteOldFiles(target);

        Assert.False(File.Exists(Path.Combine(target, "rta.exe.old")));
    }

    [Fact]
    public void InstallZip_FileOutsideOfTheFolder_Throws()
    {
        var target = Path.Combine(_folder, "app");
        Directory.CreateDirectory(target);
        var zip = CreateZip(("rta.exe", "new exe"), ("../evil.txt", "x"));

        Assert.Throws<UpdateException>(() => UpdateService.InstallZip(zip, target));
        Assert.False(File.Exists(Path.Combine(target, "rta.exe")));
        Assert.False(File.Exists(Path.Combine(_folder, "evil.txt")));
    }

    [Fact]
    public void InstallZip_WithoutExe_Throws()
    {
        var target = Path.Combine(_folder, "app");
        Directory.CreateDirectory(target);

        Assert.Throws<UpdateException>(() => UpdateService.InstallZip(CreateZip(("readme.txt", "x")), target));
    }

    private static HttpResponseMessage Json(string json) =>
        new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private static string Hash(byte[] content) => Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();

    private static ReleaseInfo Release() => new("v3.1.0", V("3.1.0"), new[]
    {
        new ReleaseAsset("rta-v3.1.0-win-x64.zip", "https://example.test/zip"),
        new ReleaseAsset("SHA256SUMS.txt", "https://example.test/sums")
    });

    private static UpdateService ServiceFor(byte[] zip, string sums) =>
        new(new HttpClient(new StubHandler(uri => uri.AbsolutePath == "/sums"
            ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(sums) }
            : new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(zip) })));

    private string CreateZip(params (string Name, string Content)[] files)
    {
        var path = Path.Combine(_folder, Guid.NewGuid().ToString("N") + ".zip");
        using var stream = File.Create(path);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Create);
        foreach (var (name, content) in files)
        {
            var entry = archive.CreateEntry(name);
            using var writer = new StreamWriter(entry.Open());
            writer.Write(content);
        }

        return path;
    }
}
