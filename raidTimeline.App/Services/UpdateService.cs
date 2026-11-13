using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace raidTimeline.App.Services;

internal sealed class UpdateException : Exception
{
    public UpdateException(string message) : base(message)
    {
    }
}

/// <summary>A version like 3.1.0 or 3.1.0-beta.1 (build metadata after a + is ignored).</summary>
internal sealed partial class SemVer : IComparable<SemVer>
{
    private readonly string[] _preRelease;

    private SemVer(int major, int minor, int patch, string[] preRelease)
    {
        Major = major;
        Minor = minor;
        Patch = patch;
        _preRelease = preRelease;
    }

    public static SemVer Zero { get; } = new(0, 0, 0, Array.Empty<string>());

    public int Major { get; }
    public int Minor { get; }
    public int Patch { get; }

    public static bool TryParse(string? text, out SemVer version)
    {
        version = null!;
        if (text == null) return false;

        var match = VersionPattern().Match(text.Trim());
        if (!match.Success) return false;

        var preRelease = match.Groups["pre"].Success
            ? match.Groups["pre"].Value.Split('.')
            : Array.Empty<string>();
        version = new SemVer(int.Parse(match.Groups["major"].Value), int.Parse(match.Groups["minor"].Value),
            int.Parse(match.Groups["patch"].Value), preRelease);
        return true;
    }

    public int CompareTo(SemVer? other)
    {
        if (other == null) return 1;

        var result = Major.CompareTo(other.Major);
        if (result != 0) return result;
        result = Minor.CompareTo(other.Minor);
        if (result != 0) return result;
        result = Patch.CompareTo(other.Patch);
        if (result != 0) return result;

        // 3.1.0-beta.1 comes before 3.1.0
        if (_preRelease.Length == 0 || other._preRelease.Length == 0)
        {
            return other._preRelease.Length.CompareTo(_preRelease.Length);
        }

        for (var i = 0; i < Math.Min(_preRelease.Length, other._preRelease.Length); i++)
        {
            var left = _preRelease[i];
            var right = other._preRelease[i];
            var leftIsNumber = int.TryParse(left, out var leftNumber);
            var rightIsNumber = int.TryParse(right, out var rightNumber);

            result = (leftIsNumber, rightIsNumber) switch
            {
                (true, true) => leftNumber.CompareTo(rightNumber),
                (true, false) => -1, // numbers come before text
                (false, true) => 1,
                _ => string.CompareOrdinal(left, right)
            };
            if (result != 0) return result;
        }

        return _preRelease.Length.CompareTo(other._preRelease.Length);
    }

    public override string ToString() =>
        $"{Major}.{Minor}.{Patch}" + (_preRelease.Length > 0 ? "-" + string.Join('.', _preRelease) : "");

    [GeneratedRegex(@"^v?(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)(-(?<pre>[0-9A-Za-z.-]+))?(\+.*)?$")]
    private static partial Regex VersionPattern();
}

internal sealed record ReleaseAsset(string Name, string Url);

internal sealed record ReleaseInfo(string Tag, SemVer Version, IReadOnlyList<ReleaseAsset> Assets)
{
    public ReleaseAsset? Find(string name) =>
        Assets.FirstOrDefault(a => string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase));
}

/// <summary>Looks for a newer release on GitHub and installs it.</summary>
internal sealed class UpdateService
{
    internal const string Repository = "danifischer/raidTimeline";
    internal const string OldFileSuffix = ".old";

    private readonly HttpClient _httpClient;
    private readonly string _latestReleaseUrl;

    public UpdateService(HttpClient httpClient, string latestReleaseUrl =
        $"https://api.github.com/repos/{Repository}/releases/latest")
    {
        _httpClient = httpClient;
        _latestReleaseUrl = latestReleaseUrl;
    }

    /// <summary>The version of the running program.</summary>
    public static SemVer CurrentVersion
    {
        get
        {
            var assembly = typeof(UpdateService).Assembly;
            var text = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                       ?? assembly.GetName().Version?.ToString(3);
            SemVer.TryParse(text, out var version);
            return version ?? SemVer.Zero;
        }
    }

    /// <summary>
    /// rta was installed with "dotnet tool install" (the program is a dll started by a shim) and not unpacked from
    /// the zip file of a release (a single file).
    /// </summary>
    public static bool IsDotnetTool(string baseDirectory) =>
        File.Exists(Path.Combine(baseDirectory, "rta.dll"));

    public static string ZipName(SemVer version) => $"rta-v{version}-win-x64.zip";

    public static string NupkgName(SemVer version) => $"rta.{version}.nupkg";

    /// <exception cref="UpdateException">GitHub did not answer with a usable release.</exception>
    public async Task<ReleaseInfo> GetLatestReleaseAsync(CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, _latestReleaseUrl);
        request.Headers.UserAgent.ParseAdd("rta-update");
        request.Headers.Accept.ParseAdd("application/vnd.github+json");

        string body;
        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new UpdateException(
                    $"GitHub answered with {(int)response.StatusCode} {response.ReasonPhrase} when asking for the latest release.");
            }

            body = await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (HttpRequestException e)
        {
            throw new UpdateException($"GitHub could not be reached: {e.Message}");
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            var tag = root.GetProperty("tag_name").GetString();
            if (!SemVer.TryParse(tag, out var version))
            {
                throw new UpdateException($"The latest release has the tag '{tag}', which is not a version.");
            }

            var assets = new List<ReleaseAsset>();
            foreach (var asset in root.GetProperty("assets").EnumerateArray())
            {
                var name = asset.GetProperty("name").GetString();
                var url = asset.GetProperty("browser_download_url").GetString();
                if (name != null && url != null) assets.Add(new ReleaseAsset(name, url));
            }

            return new ReleaseInfo(tag!, version, assets);
        }
        catch (Exception e) when (e is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            throw new UpdateException("GitHub answered with something that is not a release.");
        }
    }

    /// <summary>Downloads the asset and checks it against the SHA256SUMS.txt of the release.</summary>
    /// <exception cref="UpdateException">The files are missing, could not be downloaded or the checksum differs.</exception>
    public async Task<string> DownloadVerifiedAsync(ReleaseInfo release, string assetName, string folder,
        CancellationToken cancellationToken)
    {
        var asset = release.Find(assetName)
                    ?? throw new UpdateException($"The release {release.Tag} has no file {assetName}.");
        var sums = release.Find("SHA256SUMS.txt")
                   ?? throw new UpdateException(
                       $"The release {release.Tag} has no SHA256SUMS.txt, the download cannot be verified.");

        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, assetName);

        try
        {
            var sumsText = await _httpClient.GetStringAsync(sums.Url, cancellationToken);
            await using (var stream = await _httpClient.GetStreamAsync(asset.Url, cancellationToken))
            await using (var file = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                await stream.CopyToAsync(file, cancellationToken);
            }

            VerifyChecksum(path, sumsText);
        }
        catch (HttpRequestException e)
        {
            throw new UpdateException($"{assetName} could not be downloaded: {e.Message}");
        }

        return path;
    }

    internal static void VerifyChecksum(string path, string sumsText)
    {
        var name = Path.GetFileName(path);
        var expected = sumsText
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line => line.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Where(parts => parts.Length == 2 && string.Equals(parts[1].TrimStart('*'), name,
                StringComparison.OrdinalIgnoreCase))
            .Select(parts => parts[0])
            .FirstOrDefault()
            ?? throw new UpdateException($"SHA256SUMS.txt has no entry for {name}.");

        using var file = File.OpenRead(path);
        var actual = Convert.ToHexString(SHA256.HashData(file));
        if (!string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase))
        {
            throw new UpdateException($"The checksum of {name} does not match SHA256SUMS.txt, nothing was installed.");
        }
    }

    /// <summary>
    /// Unpacks the zip into the folder of the program. A running exe cannot be overwritten, but it can be renamed:
    /// replaced files are kept as *.old until <see cref="DeleteOldFiles"/> removes them on the next run.
    /// </summary>
    public static void InstallZip(string zipPath, string targetDirectory)
    {
        var root = Path.GetFullPath(targetDirectory) + Path.DirectorySeparatorChar;

        using var archive = ZipFile.OpenRead(zipPath);

        // check everything before touching a file
        var entries = new List<(ZipArchiveEntry Entry, string Target)>();
        foreach (var entry in archive.Entries.Where(e => !string.IsNullOrEmpty(e.Name)))
        {
            var target = Path.GetFullPath(Path.Combine(targetDirectory, entry.FullName));
            if (!target.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                throw new UpdateException($"The zip contains a file outside of the folder ({entry.FullName}).");
            }

            entries.Add((entry, target));
        }

        if (!entries.Any(e => e.Target.EndsWith("rta.exe", StringComparison.OrdinalIgnoreCase)))
        {
            throw new UpdateException("The zip does not contain rta.exe.");
        }

        var replaced = new List<(string Old, string Target)>();
        try
        {
            foreach (var (entry, target) in entries)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                if (File.Exists(target))
                {
                    var old = target + OldFileSuffix;
                    File.Move(target, old, true);
                    replaced.Add((old, target));
                }

                entry.ExtractToFile(target, true);
            }
        }
        catch
        {
            // put the old version back
            foreach (var (old, target) in replaced)
            {
                try
                {
                    File.Move(old, target, true);
                }
                catch (IOException)
                {
                }
            }

            throw;
        }
    }

    /// <summary>Removes the files an earlier update left behind.</summary>
    public static void DeleteOldFiles(string directory)
    {
        try
        {
            foreach (var file in Directory.EnumerateFiles(directory, "*" + OldFileSuffix, SearchOption.AllDirectories))
            {
                try
                {
                    File.Delete(file);
                }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException)
                {
                    // still in use, the next run tries again
                }
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
        }
    }
}
