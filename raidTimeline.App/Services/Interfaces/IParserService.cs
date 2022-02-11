using Cocona;

namespace raidTimeline.App.Services.Interfaces;

internal interface IParserService
{
    void ParseLogsFromDisk(string? day, bool reverse, bool killOnly, bool filter);
    void ParseLogsFromDiskLive(bool reverse, bool killOnly, bool filter, CoconaAppContext context);
    void ParseLogsFromDpsReport(string? day, bool reverse, bool killOnly, bool filter);
}