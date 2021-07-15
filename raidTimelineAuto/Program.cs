using raidTimelineLogic;
using raidTimelineLogic.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace raidTimelineAuto
{
	internal static class Program
	{
		private static string logPath;
		private static string outPath;

		private static void Main(string[] args)
		{
			var confPath = ParseArgs(args, "-confPath");
			var eiPath = ParseArgs(args, "-eiPath");
			var outputFileName = ParseArgs(args, "-output", "index.html");
			var reverse = Array.IndexOf(args, "-reverse") >= 0;

			if (!File.Exists(confPath))
			{
				Console.WriteLine("Non-valid conf path");
				Environment.Exit(1);
			}

			ReadConfFile(confPath);

			if (!Directory.Exists(outPath) || !Directory.Exists(logPath) || !File.Exists(eiPath))
			{
				Console.WriteLine("Non-valid ei, out or log path");
				Environment.Exit(1);
			}

			var htmlFilePath = Path.Combine(outPath, outputFileName);

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

			Task.Run(() => WatchForArcDpsFiles(new List<string>(), confPath, eiPath));
			Task.Run(() => WatchForEiFiles(new TimelineCreator(), new List<RaidModel>(), outputFileName, reverse));
			Task.Run(() => WatchConsole());

			while (true);
		}

		private static void WatchConsole()
		{
			var input = Console.ReadLine().Replace("\"", "");

			if (File.Exists(input))
			{
				var name = Path.GetFileName(input);
				var path = Path.GetFullPath(input).Replace(name, "");

				File.Move(Path.Combine(path, name), Path.Combine(path, "temp.old"));
				File.Move(Path.Combine(path, "temp.old"), Path.Combine(path, name));
			}

			WatchConsole();
		}

		private static void ReadConfFile(string confPath)
		{
			var text = File.ReadAllText(confPath);
			const string eiPathOption = "AutoAddPath=";
			const string outPathOption = "OutLocation=";

			try
			{
				logPath = text[(text.IndexOf(eiPathOption) + eiPathOption.Length)..text.IndexOf("\n", text.IndexOf(eiPathOption))];
				outPath = text[(text.IndexOf(outPathOption) + outPathOption.Length)..text.IndexOf("\n", text.IndexOf(outPathOption))];
			}
			catch
			{
				Console.WriteLine("Non-valid conf file");
			}
		}

		private static void WatchForEiFiles(TimelineCreator tc, List<RaidModel> models, string outputFileName, bool reverse)
		{
			models = tc.CreateTimelineFileFromWatching(outPath, outputFileName, models, reverse);
			var watcher = new FileSystemWatcher();
			watcher.Path = outPath;
			watcher.Filter = "*.html";
			watcher.NotifyFilter = NotifyFilters.LastWrite;
			watcher.Changed += (object source, FileSystemEventArgs e)
				=> models = tc.CreateTimelineFileFromWatching(outPath, outputFileName, models, reverse);
			watcher.EnableRaisingEvents = true;
		}

		private static void WatchForArcDpsFiles(List<string> seenFiles, string confPath, string eiPath)
		{
			var watcher = new FileSystemWatcher();
			watcher.Path = logPath;
			watcher.IncludeSubdirectories = true;
			watcher.Filter = "*.zevtc";
			watcher.Renamed += (object source, RenamedEventArgs e) =>
			{
				if (!seenFiles.Contains(e.FullPath))
				{
					seenFiles.Add(e.FullPath);
					Console.WriteLine($"Parsing raw: {e.Name} ");
					ProcessStartInfo psi = new ProcessStartInfo
					{
						FileName = eiPath,
						Arguments = $"-c \"{confPath}\" \"{e.FullPath}\"",
						UseShellExecute = false,
						RedirectStandardError = true
					};
					Process.Start(psi).WaitForExit();
				}
			};
			watcher.EnableRaisingEvents = true;
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