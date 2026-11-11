namespace raidTimeline.App.Helpers;

/// <summary>
/// Where rta reads its settings from and writes its log to.
/// </summary>
/// <param name="BaseDirectory">The folder of the program. Its files are replaced by every update.</param>
/// <param name="ConfigDirectory">The folder of the personal settings; they survive updates.</param>
/// <param name="LogDirectory">The folder of the log files.</param>
internal sealed record AppPaths(string BaseDirectory, string ConfigDirectory, string LogDirectory)
{
    internal const string HomeVariable = "RTA_HOME";
    private const string FolderName = "rta";

    /// <summary>Settings that ship with rta (defaults, e.g. the boss filter).</summary>
    internal string DefaultConfigFile => Path.Combine(BaseDirectory, "config.json");

    /// <summary>Optional overrides next to the program (older versions asked to put secrets here).</summary>
    internal string LocalConfigFile => Path.Combine(BaseDirectory, "config.local.json");

    /// <summary>The personal settings, <c>%APPDATA%\rta\config.json</c> on Windows.</summary>
    internal string UserConfigFile => Path.Combine(ConfigDirectory, "config.json");

    /// <summary>Your own boss definitions, <c>%APPDATA%\rta\mechanics.json</c> on Windows.</summary>
    internal string UserMechanicsFile => Path.Combine(ConfigDirectory, "mechanics.json");

    /// <param name="baseDirectory">The folder of the program.</param>
    /// <param name="home">
    /// Content of <see cref="HomeVariable"/>. If set, settings and logs live in that folder
    /// (e.g. for a portable installation).
    /// </param>
    internal static AppPaths Create(string baseDirectory, string? home)
    {
        if (!string.IsNullOrWhiteSpace(home))
        {
            var folder = Path.GetFullPath(home);
            return new AppPaths(baseDirectory, folder, folder);
        }

        return new AppPaths(baseDirectory,
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), FolderName),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), FolderName));
    }

    internal static AppPaths Create() =>
        Create(AppDomain.CurrentDomain.BaseDirectory, Environment.GetEnvironmentVariable(HomeVariable));
}
