using Microsoft.Extensions.Configuration;
using raidTimeline.App.Services.Interfaces;

namespace raidTimeline.App.Helpers;

internal record AppConfiguration
{
    internal string OutputPath { get; init; } = null!;
    internal string LogPath { get; init; } = null!;
    internal string ConfigurationPath { get; init; } = null!;
    internal string EliteInsightsPath { get; init; } = null!;
    internal string BossFilter { get; init; } = null!;
    internal string DpsReportToken { get; init; } = null!;
    internal string EndpointUri { get; init; } = null!;
    internal byte[] EndpointKey { get; init; } = null!;
    internal byte[] EndpointIv { get; init; } = null!;
    internal bool EliteInsightsOptionsReadable { get; init; }
}

internal class ConfigurationHelper
{
    private readonly IConfiguration _configuration;
    private readonly IEliteInsightsService _eliteInsightsService;

    public ConfigurationHelper(IConfiguration configuration, 
        IEliteInsightsService eliteInsightsService)
    {
        _configuration = configuration;
        _eliteInsightsService = eliteInsightsService;

        Configuration = LoadConfigurationValues();
    }
    
    internal AppConfiguration Configuration { get; }
    
    private AppConfiguration LoadConfigurationValues()
    {
        var endpointUri = _configuration["endpointUri"] ?? string.Empty;
        var endpointKey = _configuration["endpointKey"] ?? string.Empty;
        var endpointIv = _configuration["endpointIv"] ?? string.Empty;
        var configurationPath = _configuration["configurationPath"] ?? string.Empty;
        var eiPath = _configuration["eiPath"] ?? string.Empty;
        var bossFilter = _configuration["bossEncounterIdFilter"] ?? string.Empty;
        var dpsReportToken = _configuration["dpsReportToken"] ?? string.Empty;
        var outputPath = _configuration["outputPath"] ?? string.Empty;
        var logPath = _configuration["logPath"] ?? string.Empty;
        var eliteInsightsOptions = _eliteInsightsService
            .ReadConfigurationFile(configurationPath);

        return new AppConfiguration
        {
            DpsReportToken = dpsReportToken,
            BossFilter = bossFilter,
            EliteInsightsPath = eiPath,
            ConfigurationPath = configurationPath,
            OutputPath = eliteInsightsOptions != null ? eliteInsightsOptions.OutputPath : outputPath,
            LogPath = eliteInsightsOptions != null ? eliteInsightsOptions.LogPath : logPath,
            EliteInsightsOptionsReadable = eliteInsightsOptions != null,
            EndpointIv = Convert.FromBase64String(endpointIv),
            EndpointKey = Convert.FromBase64String(endpointKey),
            EndpointUri = endpointUri
        };
    }
}