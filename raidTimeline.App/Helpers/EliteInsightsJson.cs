using System.Text.Json;

namespace raidTimeline.App.Helpers;

internal static class EliteInsightsJson
{
    /// <summary>
    /// Reads the trigger id (the encounter id, e.g. 15438 for Vale Guardian) from an Elite Insights json file
    /// without loading the whole (often 10+ MB) file.
    /// </summary>
    internal static int? TryReadTriggerId(string jsonPath)
    {
        try
        {
            using var stream = File.OpenRead(jsonPath);
            var buffer = new byte[16 * 1024];
            var read = stream.Read(buffer, 0, buffer.Length);

            // The id is one of the first properties; scan with a reader that tolerates the cut-off at the end.
            var reader = new Utf8JsonReader(buffer.AsSpan(0, read), isFinalBlock: false, default);
            var depth = 0;
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.StartObject or JsonTokenType.StartArray:
                        depth++;
                        break;
                    case JsonTokenType.EndObject or JsonTokenType.EndArray:
                        depth--;
                        break;
                    case JsonTokenType.PropertyName when depth == 1 && reader.ValueTextEquals("triggerID"):
                        return reader.Read() && reader.TokenType == JsonTokenType.Number ? reader.GetInt32() : null;
                }
            }
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException)
        {
            // handled like a missing id
        }

        return null;
    }
}
