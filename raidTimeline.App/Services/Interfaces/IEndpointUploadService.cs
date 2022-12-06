namespace raidTimeline.App.Services.Interfaces;

internal interface IEndpointUploadService
{
    void UploadFilesToEndpoint(string? day, string raidGroup, bool killOnly, bool filter,
        CancellationToken cancellationToken);
}