using raidTimelineLogic;
using raidTimelineLogic.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace raidTimelineAuto
{
	internal static class Program
	{
		private static void Main(string[] args)
		{
			var path = ParseArgs(args, "-path");
			var outputFileName = ParseArgs(args, "-output", "index.html");
			var reverse = Array.IndexOf(args, "-reverse") >= 0;
			var htmlFilePath = Path.Combine(path, outputFileName);

			Console.CancelKeyPress += delegate
			{
				ProcessStartInfo psi = new ProcessStartInfo
				{
					FileName = htmlFilePath,
					UseShellExecute = true
				};
				Process.Start(psi);

				Environment.Exit(0);
			};

			if (!Directory.Exists(path))
			{
				Console.WriteLine("Non-valid path");
				Environment.Exit(1);
			}

			var tc = new TimelineCreator();
			var models = new List<RaidModel>();

			using (var watcher = new FileSystemWatcher())
			{
				watcher.Path = path;
				watcher.Filter = "*.html";
				watcher.NotifyFilter = NotifyFilters.LastWrite;
				watcher.Changed += (object source, FileSystemEventArgs e) =>
				{
					var oldModels = models.ConvertAll(i => i);
					models = tc.CreateTimelineFileFromWatching(path, outputFileName, models, reverse);
					var newModels = models.Where(i => !oldModels.Select(j => j.LogPath).Contains(i.LogPath));
				};
				watcher.EnableRaisingEvents = true;

				while (Console.Read() != 'q') ;
			}
		}

		private static string ParseArgs(string[] args, string search, string defaultValue = null)
		{
			var pathIndex = Array.IndexOf(args, search);

			if (pathIndex >= 0 && args.Length >= pathIndex + 1)
			{
				return args[pathIndex + 1];
			}
			else
			{
				return defaultValue;
			}
		}
	}
}