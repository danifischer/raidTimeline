namespace raidTimeline.App.Services;

internal interface IDpsReportUploadService
{
    void UploadFilesToDpsReport(string? day, bool killOnly, bool filter);
}