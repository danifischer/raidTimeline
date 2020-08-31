using raidTimelineLogic;
using raidTimelineLogic.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace raidTimelineAuto
{
	class Program
	{
		static void Main(string[] args)
		{

			var path = ParseArgs(args, "-path", @"");
			var outputFileName = ParseArgs(args, "-output", "index.html");
			var htmlFilePath = Path.Combine(path, outputFileName);
			var uploader = new SftpUpload();

			Console.CancelKeyPress += delegate {

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

			while (true)
			{
				var oldModels = models.Select(i => i).ToList();
				models = tc.CreateTimelineFileFromWatching(path, outputFileName, models);
				var newModels = models.Where(i => !oldModels.Select(j => j.LogPath).Contains(i.LogPath));

				foreach (var model in newModels)
				{
					uploader.UploadFile(model.LogPath, "/logs/upload/");
				}
				if (newModels.Any())
				{
					uploader.UploadFile(htmlFilePath, "/logs/upload/");
				}

				Thread.Sleep(2000);
			}
		}

		static string ParseArgs(string[] args, string search, string defaultValue = null)
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
