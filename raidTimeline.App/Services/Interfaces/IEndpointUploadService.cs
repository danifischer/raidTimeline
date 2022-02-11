namespace raidTimeline.App.Services;

internal interface IEndpointUploadService
{
    void UploadFilesToEndpoint(string? day, string raidGroup, bool killOnly);
}