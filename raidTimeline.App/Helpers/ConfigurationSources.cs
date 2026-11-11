using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace raidTimeline.App.Helpers;

internal static class ConfigurationSources
{
    /// <summary>The settings a user normally sets; <c>bossEncounterIdFilter</c> has a default in the shipped config.json.</summary>
    internal static readonly string[] UserKeys =
    {
        "eiPath", "configurationPath", "dpsReportToken", "outputPath", "logPath", "endpointUri", "endpointKey",
        "endpointIv"
    };

    /// <summary>
    /// Adds the settings files, a later file overrides an earlier one:
    /// the config.json next to the program (defaults), config.local.json next to the program and finally
    /// the personal config.json in the user folder.
    /// </summary>
    internal static IConfigurationBuilder AddAppConfigFiles(this IConfigurationBuilder builder, AppPaths paths)
    {
        return builder
            .AddJsonFile(paths.DefaultConfigFile, optional: false, reloadOnChange: true)
            .AddJsonFile(paths.LocalConfigFile, optional: true, reloadOnChange: true)
            .AddJsonFile(paths.UserConfigFile, optional: true, reloadOnChange: true);
    }

    /// <summary>
    /// Creates the personal settings file if it does not exist yet. It starts with the values that are currently
    /// in effect, so someone who had put them into the config.json next to the program keeps them.
    /// An existing file is never changed.
    /// </summary>
    /// <returns><c>true</c> if the file was created.</returns>
    internal static bool CreateUserConfigIfMissing(AppPaths paths, IConfiguration effective)
    {
        if (File.Exists(paths.UserConfigFile))
        {
            return false;
        }

        Directory.CreateDirectory(paths.ConfigDirectory);

        var text = new StringBuilder();
        text.AppendLine("{");
        text.AppendLine("  // Your settings for rta. They are kept when rta is updated.");
        text.AppendLine("  // Values set here override the config.json that comes with rta.");
        text.AppendLine("  // To change the boss filter add \"bossEncounterIdFilter\" (see the config.json of rta for the default).");
        text.AppendLine();
        for (var i = 0; i < UserKeys.Length; i++)
        {
            var value = JsonSerializer.Serialize(effective[UserKeys[i]] ?? string.Empty);
            text.AppendLine($"  \"{UserKeys[i]}\": {value}{(i < UserKeys.Length - 1 ? "," : string.Empty)}");
        }

        text.AppendLine("}");

        File.WriteAllText(paths.UserConfigFile, text.ToString(), new UTF8Encoding(false));
        return true;
    }
}
