using Cocona;
using Kurukuru;
using raidTimeline.App.Helpers;
using raidTimeline.App.Services.Interfaces;
using raidTimeline.Logic.Interfaces;
using raidTimeline.Logic.Models;

namespace raidTimeline.App.Services;

internal class ParserService : IParserService
{
    private readonly ITimelineCreator _timelineCreator;
    private readonly ConfigurationHelper _configurationHelper;
    private readonly IEliteInsightsService _eliteInsightsService;

    public ParserService(ITimelineCreator timelineCreator,
        ConfigurationHelper configurationHelper, 
        IEliteInsightsService eliteInsightsService)
    {
        _timelineCreator = timelineCreator;
        _configurationHelper = configurationHelper;
        _eliteInsightsService = eliteInsightsService;
    }

    public void ParseLogsFromDisk(string? day, bool reverse, bool killOnly, bool filter)
    {
        Spinner.Start($"Parsing logs for {day}", spinner =>
        {
            day ??= $"{DateTime.Now:yyyyMMdd}";
            
            if (!_configurationHelper.Configuration.EliteInsightsOptionsReadable)
            {
                spinner.Fail("Elite Insights configuration could not be read.");
            }
            
            var files = Directory.GetFiles(_configurationHelper.Configuration.LogPath,
                $"{day}*.zevtc", SearchOption.AllDirectories);

            for (var i = 0; i < files.Length; i++)
            {
                spinner.Text = $"Parsing logs for {day}: {i + 1}/{files.Length}";
                _eliteInsightsService.ParseEiFile(files[i], 
                    _configurationHelper.Configuration.ConfigurationPath, 
                    _configurationHelper.Configuration.EliteInsightsPath);
            }

            spinner.Text = $"Parsing html files";
            var models = _timelineCreator.ParseFilesFromDisk(
                _configurationHelper.Configuration.OutputPath);
            models = killOnly ? models.Where(model => model.Killed).ToList() : models;
            if (filter && !string.IsNullOrEmpty(_configurationHelper.Configuration.BossFilter))
            {
                models = models.Where(model => _configurationHelper.Configuration.BossFilter
                    .Contains(model.EncounterId.ToString("X4"))).ToList();
            }
            
            spinner.Text = $"Creating timeline";
            _timelineCreator.BuildTimelineFile(_configurationHelper.Configuration.OutputPath, 
                "index.html", models, reverse);
            
            spinner.Text = "Finished";
        });
    }

    public void ParseLogsFromDiskLive(bool reverse, bool killOnly, bool filter, CoconaAppContext context)
    {
        Spinner.Start("Starting live parsing ...", spinner =>
        {
            var arcdpsSpinner = new Spinner("arcdps watcher: starting...");
            var eliteInsightsSpinner = new Spinner("Elite Insights watcher: starting...");
            
            arcdpsSpinner.Start();
            eliteInsightsSpinner.Start();
            
            var arcDpsWatcher = WatchForArcDpsFiles(new List<string>(), arcdpsSpinner);
            var eiWatcher = WatchForEiFiles(new List<RaidModel>(), reverse, killOnly, filter, eliteInsightsSpinner);
            
            while (!context.CancellationToken.IsCancellationRequested)
            {
                spinner.Text = "Live parsing ... (press CTRL+C to stop)";
            }

            arcDpsWatcher.EnableRaisingEvents = false;
            eiWatcher.EnableRaisingEvents = false;
            
            eiWatcher.Dispose();
            arcDpsWatcher.Dispose();
            
            arcdpsSpinner.Info("arcdps watcher: stopped");
            eliteInsightsSpinner.Info("Elite Insights watcher: stopped");
            spinner.Info("Live parsing stopped.");
        });
    }
    
    private FileSystemWatcher WatchForArcDpsFiles(ICollection<string> seenFiles, Spinner spinner)
    {
        var watcher = new FileSystemWatcher();
        watcher.Path = _configurationHelper.Configuration.LogPath;
        watcher.IncludeSubdirectories = true;
        watcher.Filter = "*.zevtc";

        watcher.Renamed += (_, e) =>
        {
            if (!seenFiles.Contains(e.FullPath))
            {
                seenFiles.Add(e.FullPath);
                spinner.Text = $"arcdps watcher: parsing {e.Name}...";
                _eliteInsightsService.ParseEiFile(e.FullPath, 
                   _configurationHelper.Configuration.ConfigurationPath, 
                   _configurationHelper.Configuration.EliteInsightsPath);
                spinner.Text = "arcdps watcher: idle...";
            }
        };
        watcher.EnableRaisingEvents = true;
        spinner.Text = "arcdps watcher: idle...";
			
        return watcher;
    }
    
    private FileSystemWatcher WatchForEiFiles(IList<RaidModel> models, bool reverse, bool killOnly, 
        bool filter, Spinner spinner)
    {
        const string outputFileName = "index.html";
        models = ParseNewModels(models, reverse, killOnly, filter, outputFileName);
			
        var watcher = new FileSystemWatcher();
        watcher.Path = _configurationHelper.Configuration.OutputPath;
        watcher.Filter = "*.html";
        watcher.NotifyFilter = NotifyFilters.LastWrite;
        watcher.Changed += (_, _) =>
        {
            spinner.Text = "Elite Insights watcher: parsing new logs...";
            models = ParseNewModels(models, reverse, killOnly, filter, outputFileName);
            spinner.Text = "Elite Insights watcher: idle...";
        };
        watcher.EnableRaisingEvents = true;
        spinner.Text = "Elite Insights watcher: idle...";
			
        return watcher;
    }

    private IList<RaidModel> ParseNewModels(IList<RaidModel> models, bool reverse, bool killOnly, bool filter, string outputFileName)
    {
        // This updates the timeline regardless if the new model is filtered out or not. 
        // Think of a better logic ...
        
        var numberOfModels = models.Count;
        models = _timelineCreator
            .ParseFilesFromDiskWhileWatching(_configurationHelper.Configuration.OutputPath, outputFileName, models);

        if (models.Count > numberOfModels)
        {
            var filteredModels = killOnly ? models.Where(model => model.Killed).ToList() : models;
            if (filter && !string.IsNullOrEmpty(_configurationHelper.Configuration.BossFilter))
            {
                filteredModels = filteredModels.Where(model => _configurationHelper.Configuration.BossFilter
                    .Contains(model.EncounterId.ToString("X4"))).ToList();
            }
            
            _timelineCreator.BuildTimelineFile(_configurationHelper.Configuration.OutputPath, outputFileName,
                filteredModels, reverse);
        }

        return models;
    }


    public void ParseLogsFromDpsReport(string? day, bool reverse, bool killOnly, bool filter)
    {
        Spinner.Start($"Parsing logs for {day}", spinner =>
        {
            day ??= $"{DateTime.Now:yyyyMMdd}";

            spinner.Text = $"Downloading logs from dps.report.";
            var models = _timelineCreator.ParseFileFromWeb(
                _configurationHelper.Configuration.OutputPath, 
                _configurationHelper.Configuration.DpsReportToken, day);
            models = killOnly ? models.Where(model => model.Killed).ToList() : models;
            if (filter && !string.IsNullOrEmpty(_configurationHelper.Configuration.BossFilter))
            {
                models = models.Where(model => _configurationHelper.Configuration.BossFilter
                    .Contains(model.EncounterId.ToString("X4"))).ToList();
            }
            
            spinner.Text = $"Creating timeline";
            _timelineCreator.BuildTimelineFile(_configurationHelper.Configuration.OutputPath, 
                "index.html", models, reverse);
            
            spinner.Text = "Finished";
        });
    }
}