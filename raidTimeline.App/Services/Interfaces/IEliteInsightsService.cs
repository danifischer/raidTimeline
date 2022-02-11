namespace raidTimeline.App.Services.Interfaces;

internal interface IEliteInsightsService
{
    EliteInsightsOptions? ReadConfigurationFile(string configurationPath);

    void ParseEiFile(string logFilePath, string configurationFilePath,
        string eliteInsightsExePath);
}