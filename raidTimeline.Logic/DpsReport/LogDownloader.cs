#nullable enable
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using raidTimeline.Models.Models;

namespace raidTimeline.Logic.DpsReport;

/// <summary>
/// Loads the logs of a user from dps.report. The Elite Insights json of every log is fetched from the
/// getJson endpoint, the permalink of the log is used as link in the timeline.
/// </summary>
public class LogDownloader
{
    private const string BaseUrl = "https://dps.report/";

    private readonly HttpClient _httpClient;
    private readonly ILogger? _logger;

    public LogDownloader(ILogger<LogDownloader>? logger = null) : this(new HttpClient(), logger)
    {
    }

    internal LogDownloader(HttpClient httpClient, ILogger? logger = null)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    internal List<RaidModel> DownloadLogsForOneDay(string token, string day, CancellationToken cancellationToken)
    {
        return DownloadLogsForOneDayAsync(token, day, cancellationToken).GetAwaiter().GetResult();
    }

    internal async Task<List<RaidModel>> DownloadLogsForOneDayAsync(string token, string day,
        CancellationToken cancellationToken)
    {
        var models = new List<RaidModel>();
        var permalinks = await FetchPermalinksForOneDayAsync(day, token, cancellationToken);

        foreach (var permalink in permalinks)
        {
            if (cancellationToken.IsCancellationRequested) return models;

            _logger?.LogTrace("Loading log {Permalink}", permalink);
            var url = $"{BaseUrl}getJson?permalink={Uri.EscapeDataString(permalink)}";
            var json = await _httpClient.GetStringAsync(url, cancellationToken);

            var model = EiJsonParser.ParseJson(json, permalink, permalink, _logger);
            if (model == null)
            {
                _logger?.LogWarning("Log {Permalink} could not be parsed", permalink);
                continue;
            }

            models.Add(model);
        }

        return models;
    }

    private async Task<List<string>> FetchPermalinksForOneDayAsync(string day, string token,
        CancellationToken cancellationToken)
    {
        var page = 1;
        var maxPage = 1;
        var permalinks = new List<string>();

        while (page <= maxPage)
        {
            if (cancellationToken.IsCancellationRequested) return permalinks;

            var url = $"{BaseUrl}getUploads?userToken={Uri.EscapeDataString(token)}&page={page++}";
            var content = await _httpClient.GetStringAsync(url, cancellationToken);
            var json = JObject.Parse(content);

            maxPage = json["pages"]?.Value<int>() ?? 0;

            foreach (var upload in json["uploads"] ?? new JArray())
            {
                var date = DateTimeOffset.FromUnixTimeSeconds(upload["encounterTime"]!.Value<long>())
                    .LocalDateTime;

                if ($"{date:yyyyMMdd}" == day)
                {
                    permalinks.Add(upload["permalink"]!.Value<string>()!);
                }
            }
        }

        return permalinks;
    }
}
