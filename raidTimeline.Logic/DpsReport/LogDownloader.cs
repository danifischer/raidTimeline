using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using raidTimeline.Logic.Models;
using RestSharp;

namespace raidTimeline.Logic.DpsReport;

public class LogDownloader
{
    private readonly ILogger _logger;

    public LogDownloader(ILogger<LogDownloader> logger = null)
    {
        _logger = logger;
    }

    internal List<RaidModel> DownloadLogsForOneDay(string path, string token, string day,
        CancellationToken cancellationToken)
    {
        var httpClient = new HttpClient();
        var models = new List<RaidModel>();
        var filePath = Path.Combine(path, "test.html");
        var filteredUploads = FetchAllLogMetaDataForOneDay(day, token, cancellationToken);
        
        foreach (var upload in filteredUploads)
        {
            _logger?.LogTrace($"Loading log {upload.permalink.Value}");
            Task<string> getFileTask = httpClient.GetStringAsync(upload.permalink.Value);
            var html = getFileTask.Result;

            html = html.Replace("/cache/", "https://dps.report/cache/");
            File.WriteAllText(filePath, html);

            _logger?.LogTrace($"Parsing log {upload.permalink.Value}");
            var model = EiHtmlParser.ParseLog(filePath);
            model.LogUrl = upload.permalink.Value;
            models.Add(model);
				
            if (cancellationToken.IsCancellationRequested) return models;
        }

        File.Delete(filePath);
        return models;
    }

    private static List<dynamic> FetchAllLogMetaDataForOneDay(string day, string token, CancellationToken cancellationToken)
    {
        var client = new RestClient("https://dps.report/");
        var page = 1;
        var maxPage = 2;
        var filteredUploads = new List<dynamic>();
			
        while (page <= maxPage)
        {
            var request = new RestRequest($"getUploads?userToken={token}&page={page++}", 
                DataFormat.Json);
            var content = client.Get(request).Content;
            var json = (dynamic)JsonConvert.DeserializeObject(content);
				
            if (json == null) break;
            if (cancellationToken.IsCancellationRequested) return filteredUploads;

            maxPage = (int)json.pages.Value;

            foreach (var filteredUpload in FilterForOneDay(day, json))
            {
                filteredUploads.Add(filteredUpload);
            }
            if (cancellationToken.IsCancellationRequested) return filteredUploads;
        }

        return filteredUploads;
    }

    private static IEnumerable<dynamic> FilterForOneDay(string day, dynamic json)
    {
        foreach (var upload in json.uploads)
        {
            DateTime date = DateTimeOffset.FromUnixTimeSeconds(upload.encounterTime.Value)
                .LocalDateTime;
            var dateString = $"{date:yyyyMMdd}";

            if (dateString == day)
            {
                yield return upload;
            }
        }
    }
}