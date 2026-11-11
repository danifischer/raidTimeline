#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using raidTimeline.Models.Models;

namespace raidTimeline.Logic.DpsReport;

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

    internal List<RaidModel> DownloadLogsForOneDay(string path, string token, string day,
        CancellationToken cancellationToken)
    {
        return DownloadLogsForOneDayAsync(path, token, day, cancellationToken).GetAwaiter().GetResult();
    }

    internal async Task<List<RaidModel>> DownloadLogsForOneDayAsync(string path, string token, string day,
        CancellationToken cancellationToken)
    {
        var models = new List<RaidModel>();
        var uploads = await FetchAllLogMetaDataForOneDayAsync(day, token, cancellationToken);

        foreach (var upload in uploads)
        {
            if (cancellationToken.IsCancellationRequested) return models;

            var permalink = upload["permalink"]!.Value<string>()!;
            _logger?.LogTrace("Loading log {Permalink}", permalink);
            var html = await _httpClient.GetStringAsync(permalink, cancellationToken);
            html = html.Replace("/cache/", "https://dps.report/cache/");

            // Unique file per log so parallel runs and leftovers cannot interfere with each other.
            var filePath = Path.Combine(path, $"{Guid.NewGuid():N}.html");
            try
            {
                await File.WriteAllTextAsync(filePath, html, cancellationToken);

                _logger?.LogTrace("Parsing log {Permalink}", permalink);
                var model = EiHtmlParser.ParseLog(filePath, _logger);
                if (model == null)
                {
                    _logger?.LogWarning("Log {Permalink} could not be parsed", permalink);
                    continue;
                }

                model.LogUrl = permalink;
                models.Add(model);
            }
            finally
            {
                File.Delete(filePath);
            }
        }

        return models;
    }

    private async Task<List<JToken>> FetchAllLogMetaDataForOneDayAsync(string day, string token,
        CancellationToken cancellationToken)
    {
        var page = 1;
        var maxPage = 1;
        var filteredUploads = new List<JToken>();

        while (page <= maxPage)
        {
            if (cancellationToken.IsCancellationRequested) return filteredUploads;

            var url = $"{BaseUrl}getUploads?userToken={Uri.EscapeDataString(token)}&page={page++}";
            var content = await _httpClient.GetStringAsync(url, cancellationToken);
            var json = JObject.Parse(content);

            maxPage = json["pages"]?.Value<int>() ?? 0;

            foreach (var upload in FilterForOneDay(day, json))
            {
                filteredUploads.Add(upload);
            }
        }

        return filteredUploads;
    }

    private static IEnumerable<JToken> FilterForOneDay(string day, JObject json)
    {
        foreach (var upload in json["uploads"] ?? new JArray())
        {
            var date = DateTimeOffset.FromUnixTimeSeconds(upload["encounterTime"]!.Value<long>())
                .LocalDateTime;

            if ($"{date:yyyyMMdd}" == day)
            {
                yield return upload;
            }
        }
    }
}
