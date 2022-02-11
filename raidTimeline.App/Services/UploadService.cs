using raidTimeline.App.Services.Interfaces;

namespace raidTimeline.App.Services;

internal class UploadService : IUploadService
{
    private readonly IEndpointUploadService _endpointUploadService;
    private readonly IDpsReportUploadService _dpsReportUploadService;

    public UploadService(IEndpointUploadService endpointUploadService, 
        IDpsReportUploadService dpsReportUploadService)
    {
        _endpointUploadService = endpointUploadService;
        _dpsReportUploadService = dpsReportUploadService;
    }

    public void UploadFilesToDpsReport(string? day, bool killOnly, bool filter)
    {
       _dpsReportUploadService.UploadFilesToDpsReport(day, killOnly, filter);
    }

    public void UploadFilesToEndpoint(string? day, string raidGroup, bool killOnly)
    {
        _endpointUploadService.UploadFilesToEndpoint(day, raidGroup, killOnly);
    }
}