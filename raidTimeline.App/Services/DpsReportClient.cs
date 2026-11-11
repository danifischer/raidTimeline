using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace raidTimeline.App.Services;

/// <summary>The result of uploading a log to dps.report.</summary>
/// <param name="Permalink">Link to the report.</param>
/// <param name="Success">The boss was killed.</param>
/// <param name="BossId">The id of the boss (same ids as the arcdps.ini uses).</param>
internal sealed record DpsReportUpload(string Permalink, bool Success, int BossId);

internal sealed class DpsReportException : Exception
{
    public DpsReportException(string message, HttpStatusCode? statusCode = null) : base(message)
    {
        StatusCode = statusCode;
    }

    public HttpStatusCode? StatusCode { get; }
}

/// <summary>Uploads arcdps logs to https://dps.report (see https://dps.report/api).</summary>
internal sealed class DpsReportClient
{
    private const string UploadUrl = "https://dps.report/uploadContent";

    private readonly HttpClient _httpClient;

    public DpsReportClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <exception cref="DpsReportException">dps.report did not accept the log.</exception>
    /// <exception cref="HttpRequestException">dps.report could not be reached.</exception>
    public async Task<DpsReportUpload> UploadAsync(string path, string userToken, CancellationToken cancellationToken)
    {
        var url = $"{UploadUrl}?userToken={Uri.EscapeDataString(userToken)}&generator=ei&json=1";

        await using var file = File.OpenRead(path);
        using var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(file);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(fileContent, "file", Path.GetFileName(path));

        using var response = await _httpClient.PostAsync(url, content, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(body);
        }
        catch (JsonException)
        {
            // e.g. an html error page of a proxy
            throw new DpsReportException(
                $"dps.report answered with {(int)response.StatusCode} {response.ReasonPhrase} and no json.",
                response.StatusCode);
        }

        using (document)
        {
            var root = document.RootElement;
            var error = root.ValueKind == JsonValueKind.Object && root.TryGetProperty("error", out var e)
                ? e.GetString()
                : null;

            if (!string.IsNullOrEmpty(error))
            {
                throw new DpsReportException(error, response.StatusCode);
            }

            if (!response.IsSuccessStatusCode || root.ValueKind != JsonValueKind.Object
                                              || !root.TryGetProperty("permalink", out var permalink)
                                              || string.IsNullOrEmpty(permalink.GetString()))
            {
                throw new DpsReportException(
                    $"dps.report answered with {(int)response.StatusCode} {response.ReasonPhrase} and no report.",
                    response.StatusCode);
            }

            return new DpsReportUpload(permalink.GetString()!, ReadSuccess(root), ReadBossId(root));
        }
    }

    private static bool ReadSuccess(JsonElement root) =>
        root.TryGetProperty("encounter", out var encounter) && encounter.ValueKind == JsonValueKind.Object
                                                            && encounter.TryGetProperty("success", out var success)
                                                            && success.ValueKind == JsonValueKind.True;

    private static int ReadBossId(JsonElement root) =>
        root.TryGetProperty("evtc", out var evtc) && evtc.ValueKind == JsonValueKind.Object
                                                  && evtc.TryGetProperty("bossId", out var bossId)
                                                  && bossId.TryGetInt32(out var id)
            ? id
            : 0;
}
