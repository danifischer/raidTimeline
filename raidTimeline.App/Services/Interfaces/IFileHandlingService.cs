namespace raidTimeline.App.Services.Interfaces;

internal interface IFileHandlingService
{
    void PrintHtmlFileSize();
    void PrintLogFileSize();
    void DeleteHtmlAllFiles();
    void DeleteLogFiles(int? days, bool all, bool force);
    void OpenTimeline();
}