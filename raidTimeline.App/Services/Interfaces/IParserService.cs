using Cocona;

namespace raidTimeline.App.Services.Interfaces;

internal interface IParserService
{
    void ParseLogsFromDisk(string? day, bool reverse, bool killOnly, bool filter, 
        CancellationToken cancellationToken);
    void ParseLogsFromDiskLive(bool reverse, bool killOnly, bool filter, 
        CancellationToken cancellationToken);
    void ParseLogsFromDpsReport(string? day, bool reverse, bool killOnly, bool filter, 
        CancellationToken cancellationToken);
}