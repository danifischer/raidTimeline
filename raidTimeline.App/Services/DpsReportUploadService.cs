using System.Collections.Concurrent;
using Kurukuru;
using Microsoft.Extensions.Logging;
using raidTimeline.App.Helpers;

namespace raidTimeline.App.Services;

internal class DpsReportUploadService : IDpsReportUploadService
{
    private readonly ConfigurationHelper _configurationHelper;
    private readonly DpsReportClient _client;
    private readonly ILogger<DpsReportUploadService> _logger;

    public DpsReportUploadService(ConfigurationHelper configurationHelper, DpsReportClient client,
        ILogger<DpsReportUploadService> logger)
    {
        _configurationHelper = configurationHelper;
        _client = client;
        _logger = logger;
    }

    public void UploadFilesToDpsReport(string? day, bool killOnly, bool filter,
        CancellationToken cancellationToken)
    {
        Spinner.Start($"Uploading logs to dps.report", spinner =>
        {
            day ??= $"{DateTime.Now:yyyyMMdd}";
            spinner.Text = $"Uploading logs to dps.report for {day}";

            var files = Directory.GetFiles(_configurationHelper.Configuration.LogPath,
                $"{day}*.zevtc", SearchOption.AllDirectories);

            var done = 0;
            var failed = 0;

            spinner.Text = files.Length != 0
                ? $"Uploading logs to dps.report for {day} ({done}/{files.Length})"
                : $"No files to upload for {day}.";

            var links = new ConcurrentBag<(string File, string Link)>();
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = 3,
                CancellationToken = cancellationToken
            };

            try
            {
                Parallel.ForEachAsync(files, parallelOptions, async (file, token) =>
                {
                    try
                    {
                        var upload = await _client.UploadAsync(file, _configurationHelper.Configuration.DpsReportToken,
                            token);

                        if (IsWanted(upload, killOnly, filter, _configurationHelper.Configuration.BossFilter))
                        {
                            links.Add((file, upload.Permalink));
                        }
                    }
                    catch (Exception e) when (e is DpsReportException or HttpRequestException or IOException)
                    {
                        Interlocked.Increment(ref failed);
                        _logger.LogError("Upload failed for {File}: {Message}", file, e.Message);
                    }
                    finally
                    {
                        Interlocked.Increment(ref done);
                        spinner.Text = $"Uploading logs to dps.report for {day} ({done}/{files.Length})";
                    }
                }).GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
                spinner.Info("Upload canceled.");
            }

            Console.WriteLine(string.Join('\n', links.OrderBy(l => l.File, StringComparer.Ordinal).Select(l => l.Link)));
            if (failed > 0)
            {
                spinner.Warn($"{failed} of {files.Length} logs could not be uploaded (see the log file).");
            }
        });
    }

    /// <summary>Whether the link of an uploaded log is printed.</summary>
    /// <param name="bossFilter">Hexadecimal boss ids as in the config, e.g. "0x3C4E,0x3C45".</param>
    internal static bool IsWanted(DpsReportUpload upload, bool killOnly, bool filter, string? bossFilter)
    {
        if (killOnly && !upload.Success)
        {
            return false;
        }

        if (!filter)
        {
            return true;
        }

        return !string.IsNullOrEmpty(bossFilter) && bossFilter.Contains(upload.BossId.ToString("X4"), StringComparison.OrdinalIgnoreCase);
    }
}
