using Cocona;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using raidTimeline.App.Commands;
using raidTimeline.App.Helpers;
using raidTimeline.App.Services;
using raidTimeline.App.Services.Interfaces;
using raidTimeline.Logic;
using raidTimeline.Logic.Interfaces;

var builder = CoconaApp.CreateBuilder();

builder.Configuration.AddJsonFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json"), 
    false, true);
builder.Services.AddSingleton<ConfigurationHelper>();
builder.Services.AddTransient<IEliteInsightsService, EliteInsightsService>();
builder.Services.AddTransient<ITimelineCreator, TimelineCreator>();
builder.Services.AddTransient<IParserService, ParserService>();
builder.Services.AddTransient<IFileHandlingService, FileHandlingService>();
builder.Services.AddTransient<IUploadService, UploadService>();
builder.Services.AddTransient<IEndpointUploadService, EndpointUploadService>();
builder.Services.AddTransient<IDpsReportUploadService, DpsReportUploadService>();

var app = builder.Build();

app.RegisterParserCommands();
app.RegisterFileCommands();
app.RegisterUploadCommands();

app.Run();