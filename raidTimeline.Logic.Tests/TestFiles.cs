using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Xunit;

namespace raidTimeline.Logic.Tests;

internal static class TestFiles
{
    /// <summary>
    /// Elite Insights json (v3.32) of a Vale Guardian kill with 10 players, created from
    /// <c>test_log.zevtc</c> and shrunk with <c>trim_ei_json.py</c>.
    /// </summary>
    internal static string ValeGuardianLog => Path.Combine(AppContext.BaseDirectory, "Files", "vale_guardian_kill.json");

    /// <summary>Asserts that the dictionary contains each key with exactly the given count.</summary>
    internal static void AssertCounts(IReadOnlyDictionary<string, int> actual, params (string Key, int Count)[] expected)
    {
        foreach (var (key, count) in expected)
        {
            Assert.True(actual.TryGetValue(key, out var value), $"'{key}' is missing");
            Assert.True(count == value, $"'{key}': expected {count}, but was {value}");
        }
    }

    /// <summary>Creates a fresh temp directory which is deleted when the returned object is disposed.</summary>
    internal static TempDirectory CreateTempDirectory() => new();

    /// <summary>
    /// Copies the fixture to <paramref name="target"/> after applying <paramref name="modify"/> to its json,
    /// so tests can simulate other Elite Insights output.
    /// </summary>
    internal static string WriteModifiedLog(string target, Action<JObject> modify)
    {
        var json = JObject.Parse(File.ReadAllText(ValeGuardianLog));
        modify(json);
        File.WriteAllText(target, json.ToString(Newtonsoft.Json.Formatting.None));
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
