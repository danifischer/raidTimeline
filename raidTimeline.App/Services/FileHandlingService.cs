using System.Diagnostics;
using Kurukuru;
using raidTimeline.App.Helpers;
using raidTimeline.App.Services.Interfaces;
using UnitsNet;

namespace raidTimeline.App.Services;

internal class FileHandlingService : IFileHandlingService
{
    private readonly ConfigurationHelper _configurationHelper;

    public FileHandlingService(ConfigurationHelper configurationHelper)
    {
        _configurationHelper = configurationHelper;
    }

    public void PrintHtmlFileSize()
    {
        PrintSizeOfFilesInADirectory(_configurationHelper.Configuration.OutputPath);
    }

    public void PrintLogFileSize()
    {
        PrintSizeOfFilesInADirectory(_configurationHelper.Configuration.LogPath);
    }

    public void DeleteHtmlAllFiles()
    {
        DeleteAllFilesInDirectory(_configurationHelper.Configuration.OutputPath);
    }

    public void DeleteLogFiles(int? days, bool all, bool force)
    {
        Spinner.Start("Deleting files.", spinner =>
        {
            var files = GetFiles(_configurationHelper.Configuration.LogPath);
            files = FilterFiles(days, all, force, spinner, files);
            DeleteFiles(spinner, files);
        });
    }

    public void OpenTimeline()
    {
        Spinner.Start("Opening timeline file.", spinner =>
        {
            var path = Path.Combine(_configurationHelper.Configuration.OutputPath,
                "index.html");

            if (!File.Exists(path))
            {
                spinner.Fail("File not found.");
            }

            var processStartInfo = new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            };
            Process.Start(processStartInfo);
        });
    }

    private static FileInfo[] FilterFiles(int? days, bool all, bool force, Spinner spinner, FileInfo[] files)
    {
        if (!all)
        {
            if (days == null)
            {
                spinner.Fail("This selection needs the number of days you want to delete.\n" +
                             "Please use the -d or -a option.");
            }

            var dayLimit = DateTime.Now.AddDays(days!.Value * -1);
            files = files.Where(file => file.CreationTime.Date < dayLimit.Date).ToArray();
        }

        var fileCount = files.Length;

        if (fileCount > 50 && !force)
        {
            spinner.Fail("More than 50 files would be deleted with this option.\n" +
                         "Please use the command again with the -f option to delete.");
        }

        return files;
    }

    private static void DeleteAllFilesInDirectory(string directory)
    {
        Spinner.Start("Deleting files.", spinner =>
        {
            var files = GetFiles(directory);
            DeleteFiles(spinner, files);
        });
    }

    private static void DeleteFiles(Spinner spinner, IReadOnlyList<FileInfo> files)
    {
        spinner.Text = $"Deleting {files.Count} files.";

        for (var i = 0; i < files.Count; i++)
        {
            spinner.Text = $"Deleted {i}/{files.Count} files.";
            files[i].Delete();
        }

        spinner.Text = $"Finished deleting {files.Count} files.";
    }

    private static FileInfo[] GetFiles(string directory)
    {
        var directoryInfo = new DirectoryInfo(directory);
        return directoryInfo.GetFiles("*", SearchOption.AllDirectories);
    }

    private static void PrintSizeOfFilesInADirectory(string directory)
    {
        Spinner.Start("Calculating file size", spinner =>
        {
            var files = GetFiles(directory);

            var totalSize = files.Sum(file => file.Length);
            var bytes = Information.FromBytes(totalSize);

            spinner.Text = $"{files.Length} files found taking {bytes.Mebibytes:F} MB of disk space";
        });
    }
}