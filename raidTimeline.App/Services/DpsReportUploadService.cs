using System.Net;
using Kurukuru;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using raidTimeline.App.Helpers;
using RestSharp;

namespace raidTimeline.App.Services;

internal class DpsReportUploadService : IDpsReportUploadService
{
    private readonly ConfigurationHelper _configurationHelper;
    private readonly ILogger<DpsReportUploadService> _logger;

    public DpsReportUploadService(ConfigurationHelper configurationHelper, 
        ILogger<DpsReportUploadService> logger)
    {
        _configurationHelper = configurationHelper;
        _logger = logger;
    }
    
    public void UploadFilesToDpsReport(string? day, bool killOnly, bool filter)
    {
        Spinner.Start($"Uploading logs to dps.report", spinner =>
        {
            day ??= $"{DateTime.Now:yyyyMMdd}";
            spinner.Text = $"Uploading logs to dps.report for {day}";

            var files = Directory.GetFiles(_configurationHelper.Configuration.LogPath,
                $"{day}*.zevtc", SearchOption.AllDirectories);

            var count = 0;

            spinner.Text = files.Length != 0
                ? $"Uploading logs to dps.report for {day} ({count}/{files.Length})"
                : $"No files to upload for {day}.";

            var links = new List<string>();

            Parallel.ForEach(files,
                new ParallelOptions {MaxDegreeOfParallelism = 3},
                file =>
                {
                    try
                    {
                        var response = UploadToDpsReport(file);
                        if (response.StatusCode != HttpStatusCode.OK)
                        {
                            _logger.LogError($"Upload Failed for {file}");
                        }

                        dynamic json = JsonConvert.DeserializeObject(response.Content)!;
                        if ((!killOnly || json.encounter.success.Value == true)
                            && (!filter ||
                                (!string.IsNullOrEmpty(_configurationHelper.Configuration.BossFilter)
                                 && _configurationHelper.Configuration.BossFilter.Contains(
                                     json.evtc.bossId.ToString("X4")))))
                        {
                            links.Add(json.permalink.Value.ToString());
                        }
                    }
                    finally
                    {
                        Interlocked.Increment(ref count);
                        spinner.Text = $"Uploading logs to dps.report for {day} ({count}/{files.Length})";
                    }
                });

            Console.WriteLine(string.Join("\n", links));
        });
    }
    
    private IRestResponse UploadToDpsReport(string path)
    {
        var client = new RestClient("https://dps.report/");

        var request = new RestRequest(
            $"uploadContent?userToken={_configurationHelper.Configuration.DpsReportToken}&generator=ei&json=1",
            DataFormat.Json);
        request.AddHeader("Content-Type", "multipart/form-data");
        request.AddFile("file", path);

        return client.Post(request);
    }
}