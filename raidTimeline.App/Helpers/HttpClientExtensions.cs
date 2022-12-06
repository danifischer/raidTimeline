namespace raidTimeline.App.Helpers;

public static class HttpClientExtensions
{
    public static async Task DownloadFileAsync(this HttpClient client, Uri uri, string path)
    {
        await using var s = await client.GetStreamAsync(uri);
        await using var fs = new FileStream(path, FileMode.CreateNew);
        await s.CopyToAsync(fs);
    }
}