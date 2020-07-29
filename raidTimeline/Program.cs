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
			string path = "";

			if (args.Length == 1)
			{
				path = args[0];
				if (!Directory.Exists(path))
				{
					Console.WriteLine("Wrong Path");
					Environment.Exit(1);
				}
			}
			else
			{
				Console.WriteLine("No Path");
				Environment.Exit(1);
			}

			new TimelineCreator().CreateTimelineFile(path, "raid.html");

			var htmlFilePath = Path.Combine(path, "raid.html");

			ProcessStartInfo psi = new ProcessStartInfo
			{
				FileName = htmlFilePath,
				UseShellExecute = true
			};
			Process.Start(psi);
		}
	}
}
