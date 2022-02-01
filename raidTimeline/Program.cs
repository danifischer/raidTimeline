using raidTimelineLogic;
using System;
using System.Diagnostics;
using System.IO;

namespace raidTimeline
{
	static class Program
	{
		static void Main(string[] args)
		{
			var path = ParseArgs(args, "-path");
			var outputFileName = ParseArgs(args, "-output", "index.html");
			var token = ParseArgs(args, "-token");
			var number = ParseArgs(args, "-number", "19");
			var reverse = Array.IndexOf(args, "-reverse") >= 0;

			if(!Directory.Exists(path))
			{
				Console.WriteLine("Non-valid path");
				Environment.Exit(1);
			}

			var tc = new TimelineCreator();

			var models = token != null 
				? tc.ParseFileFromWeb(path, token, int.Parse(number)) 
				: tc.ParseFilesFromDisk(path);

			tc.BuildTimelineFile(path, outputFileName, models, reverse);
			var htmlFilePath = Path.Combine(path, outputFileName);

			ProcessStartInfo psi = new ProcessStartInfo
			{
				FileName = htmlFilePath,
				UseShellExecute = true
			};
			Process.Start(psi);
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
