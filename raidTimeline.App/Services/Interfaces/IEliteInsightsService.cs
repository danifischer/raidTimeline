namespace raidTimeline.App.Services.Interfaces;

internal interface IEliteInsightsService
{
    EliteInsightsOptions? ReadConfigurationFile(string configurationPath);

    /// <summary>Runs Elite Insights on the log and returns when it is finished.</summary>
    /// <returns>False if Elite Insights could not be started or did not finish successfully.</returns>
    bool ParseEiFile(string logFilePath, string configurationFilePath, string eliteInsightsExePath,
        CancellationToken cancellationToken = default);
}