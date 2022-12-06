﻿using System.Net;
using System.Security.Cryptography;
using Kurukuru;
using Microsoft.Extensions.Logging;
using raidTimeline.App.Helpers;
using raidTimeline.App.Services.Interfaces;

namespace raidTimeline.App.Services;

internal class EndpointUploadService : IEndpointUploadService
{
    private readonly ConfigurationHelper _configurationHelper;
    private readonly ILogger<EndpointUploadService> _logger;

    public EndpointUploadService(ConfigurationHelper configurationHelper, 
        ILogger<EndpointUploadService> logger)
    {
        _configurationHelper = configurationHelper;
        _logger = logger;
    }

    public void UploadFilesToEndpoint(string? day, string raidGroup, bool killOnly, bool filter,
        CancellationToken cancellationToken)
    {
        Spinner.Start("Uploading files to endpoint.", spinner =>
        {
            var date = DateTime.Now;
            if (day != null)
            {
                date = new DateTime(
                    Convert.ToInt32(day.Substring(0, 4)),
                    Convert.ToInt32(day.Substring(4, 2)),
                    Convert.ToInt32(day.Substring(6, 2)));
            }

            day ??= $"{date:yyyyMMdd}";
            spinner.Text = $"Uploading html files to endpoint for {day}";

            var files = Directory.GetFiles(_configurationHelper.Configuration.OutputPath,
                $"{day}*.html", SearchOption.AllDirectories);

            files = killOnly
                ? files.Where(file => file.EndsWith("kill.html")).ToArray()
                : files;
            
            // filter for id
            // get all text and scan for '"fightId": xxx,'?
            
            var filteredFiles = new List<string>();
            
            foreach (var file in files)
            {
                const string searchString = "\"fightID\":";
                var text = File.ReadAllText(file);

                var start = text.IndexOf(searchString, StringComparison.Ordinal) + searchString.Length;
                var end = text.IndexOf(",", start, StringComparison.Ordinal);

                var id = Convert.ToInt32(text.Substring(start, end - start));

                if (!filter || _configurationHelper.Configuration.BossFilter.Contains(id.ToString("X4")))
                {
                    filteredFiles.Add(file);
                }
            }

            var uploadOk = 0;
            var uploadFail = 0;
            
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = 3,
                CancellationToken = cancellationToken
            };

            Parallel.ForEach(filteredFiles, parallelOptions, file =>
            {
                spinner.Text = $"Uploading {filteredFiles.Count} files: {uploadOk} ok, {uploadFail} failed...";
                if (new FileInfo(file).Length == 0)
                {
                    Interlocked.Increment(ref uploadFail);
                    _logger.LogError("Log file is 0 bytes");
                    return;
                }
                var uploadResponse = UploadFileToEndpoint(file, raidGroup, date).Result;

                if (uploadResponse.StatusCode != HttpStatusCode.OK)
                {
                    Interlocked.Increment(ref uploadFail);
                    _logger.LogError(uploadResponse.ReasonPhrase);
                }
                else
                {
                    Interlocked.Increment(ref uploadOk);
                }
            });

            var resultUri = TriggerTimelineCreation(raidGroup, date, spinner, files, uploadOk, uploadFail);

            spinner.Text = $"Tried uploading {filteredFiles.Count} files: {uploadOk} ok, {uploadFail} failed.\n" +
                           $"Timeline creation successful: {resultUri}";
        });
    }

    private Uri TriggerTimelineCreation(string raidGroup, DateTime date, Spinner spinner, string[] files, int uploadOk,
        int uploadFail)
    {
        var uri = new Uri($"{_configurationHelper.Configuration.EndpointUri}/{raidGroup}/{date.Date:dd.MM.yyyy}");
        var client = new HttpClient();
        spinner.Text = $"Creating timeline...";
        var timelineResponse = client.PostAsync(uri, null).Result;
        
        if (timelineResponse.StatusCode != HttpStatusCode.OK)
        {
            spinner.Fail($"Tried uploading {files.Length} files: {uploadOk} ok, {uploadFail} failed.\n" +
                         $"Timeline creation failed!");
            _logger.LogError(timelineResponse.ReasonPhrase);
        }

        var location = timelineResponse.Content.ReadAsStringAsync().Result;
        return new Uri(location);
    }

    private async Task<HttpResponseMessage> UploadFileToEndpoint(string logfile, string raidGroup, DateTime date)
    {
        await using var fileStream = new FileStream(logfile, FileMode.Open);

        var uri = new Uri(
            $"{_configurationHelper.Configuration.EndpointUri}/{raidGroup}/{date.Date:dd.MM.yyyy}/{Path.GetFileName(logfile)}");
        var client = new HttpClient();

        await using var encryptedStream = EncryptStream(fileStream);

        HttpContent content = new StreamContent(encryptedStream);
        return await client.PutAsync(uri, content).ConfigureAwait(false);
    }

    private Stream EncryptStream(Stream stream)
    {
        using var rmCrypto = new RijndaelManaged();
        var encryptor = rmCrypto.CreateEncryptor(_configurationHelper.Configuration.EndpointKey,
            _configurationHelper.Configuration.EndpointIv);

        rmCrypto.Padding = PaddingMode.PKCS7;

        var memoryStream = new MemoryStream();
        var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);
        stream.Position = 0;
        stream.CopyTo(cryptoStream);
        cryptoStream.FlushFinalBlock();
        memoryStream.Position = 0;
        return memoryStream;
    }
}