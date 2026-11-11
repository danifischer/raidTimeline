using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace raidTimeline.Logic.Tests;

internal static class TestFiles
{
    private const string LogDataMarker = "var _logData = ";

    /// <summary>Elite Insights html (older format) of a Soulless Horror kill with 10 players.</summary>
    internal static string ParsedLog => Path.Combine(AppContext.BaseDirectory, "Files", "parsed_test_log.html");

    /// <summary>Creates a fresh temp directory which is deleted when the returned object is disposed.</summary>
    internal static TempDirectory CreateTempDirectory() => new();

    /// <summary>
    /// Copies the fixture to <paramref name="target"/> after applying <paramref name="modify"/> to its embedded
    /// <c>_logData</c> json, so tests can simulate other Elite Insights output formats.
    /// </summary>
    internal static string WriteModifiedLog(string target, Action<JObject> modify)
    {
        var lines = File.ReadAllLines(ParsedLog);
        var index = Array.FindIndex(lines, l => l.Contains(LogDataMarker));
        var line = lines[index];

        var start = line.IndexOf(LogDataMarker, StringComparison.Ordinal);
        var end = line.IndexOf("]};", start, StringComparison.Ordinal) + 2;
        var json = JObject.Parse(line[(start + LogDataMarker.Length)..end]);
        modify(json);

        lines[index] = line[..start] + LogDataMarker + json.ToString(Newtonsoft.Json.Formatting.None) + ";";
        File.WriteAllLines(target, lines);
        return target;
    }

    internal sealed class TempDirectory : IDisposable
    {
        internal TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "rta-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        internal string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path)) Directory.Delete(Path, true);
        }
    }
}
