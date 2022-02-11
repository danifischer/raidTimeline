namespace raidTimeline.App.Services.Interfaces;

internal interface IUploadService
{
    void UploadFilesToDpsReport(string? day, bool killOnly, bool filter);
    void UploadFilesToEndpoint(string? day, string raidGroup, bool killOnly);
}