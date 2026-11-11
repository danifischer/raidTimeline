using Cocona;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using raidTimeline.App.Commands;
using raidTimeline.App.Helpers;
using raidTimeline.App.Services;
using raidTimeline.App.Services.Interfaces;
using raidTimeline.Database;
using raidTimeline.Logic;
using raidTimeline.Logic.Interfaces;
using Serilog;


var builder = CoconaApp.CreateBuilder();

// config.json next to the program (defaults) < config.local.json next to the program < your settings file in the
// user folder (survives updates, see "rta config edit").
var paths = AppPaths.Create();
builder.Configuration.AddAppConfigFiles(paths);
builder.Services.AddSingleton(paths);

var serilog = new LoggerConfiguration()
    .MinimumLevel.Verbose()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.File(Path.Combine(paths.LogDirectory, "rta_log.txt"), rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7)
    .CreateLogger();

builder.Logging.AddSerilog(serilog);

builder.Services.AddSingleton<ConfigurationHelper>();
builder.Services.AddTransient<IEliteInsightsService, EliteInsightsService>();
builder.Services.AddTransient<ITimelineCreator, TimelineCreator>();
builder.Services.AddTransient<IParserService, ParserService>();
builder.Services.AddTransient<IFileHandlingService, FileHandlingService>();
builder.Services.AddTransient<IUploadService, UploadService>();
builder.Services.AddTransient<IEndpointUploadService, EndpointUploadService>();
builder.Services.AddTransient<IDpsReportUploadService, DpsReportUploadService>();
builder.Services.RegisterDatabaseServices();

var app = builder.Build();

app.RegisterConfigCommands();
app.RegisterParserCommands();
app.RegisterFileCommands();
app.RegisterUploadCommands();
app.RegisterDatabaseCommands();

app.Run();