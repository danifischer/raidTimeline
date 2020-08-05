using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.IO;
using System.Linq;
using raidTimelineLogic;
using FluentAssertions;

namespace raidTimelineLogicTests
{
	[TestClass]
	public class IntegrationTests
	{
		private static readonly string TestFile = Path.Combine(Directory.GetCurrentDirectory(), @"Files\test_log.zevtc");

		[TestMethod]
		public void ParseLog_Latest_ShouldParse()
		{
			// Prepare
			ProcessStartInfo psi1 = new ProcessStartInfo
			{
				FileName = @"Files\fetchAndBuildEi.cmd",
				UseShellExecute = true
			};
			Process.Start(psi1).WaitForExit();

			var pathToEi = Path.Combine(Directory.GetCurrentDirectory(), @"GW2-Elite-Insights-Parser\GW2EI.bin\Debug\GuildWars2EliteInsights.exe");
			var htmlFile = CreateHtmlLog(pathToEi);
			var parser = new EiHtmlParser();

			// Test
			var log = parser.ParseLog(htmlFile);

			// Check
			log.Should().NotBeNull();

			// Cleanup
			File.Delete(htmlFile);
			DeleteDirectory("GW2-Elite-Insights-Parser");
		}

		[DataTestMethod]
		[DataRow(@"EliteInsightsVersions\2.25.0.0\GuildWars2EliteInsights.exe", DisplayName = "Version 2.25.")]
		[DataRow(@"EliteInsightsVersions\2.26.0.0\GuildWars2EliteInsights.exe", DisplayName = "Version 2.26.")]
		public void ParseLog_SpecificEliteInsightVersion_ShouldParse(string pathToEi)
		{
			// Prepare
			var htmlFile = CreateHtmlLog(pathToEi);
			var parser = new EiHtmlParser();

			// Test
			var log = parser.ParseLog(htmlFile);

			// Check
			log.Should().NotBeNull();

			// Cleanup
			File.Delete(htmlFile);
		}

		private static string CreateHtmlLog(string pathToEi)
		{
			string htmlFile;
			ProcessStartInfo psi = new ProcessStartInfo
			{
				FileName = pathToEi,
				Arguments = $"-p {TestFile}",
				UseShellExecute = true
			};
			Process.Start(psi).WaitForExit();

			htmlFile = Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "Files"), "test_log*.html").Single();
			return htmlFile;
		}

		// This is needed as Directory.Delete is stupid ... 
		public void DeleteDirectory(string targetDir)
		{
			File.SetAttributes(targetDir, FileAttributes.Normal);

			string[] files = Directory.GetFiles(targetDir);
			string[] dirs = Directory.GetDirectories(targetDir);

			foreach (string file in files)
			{
				File.SetAttributes(file, FileAttributes.Normal);
				File.Delete(file);
			}

			foreach (string dir in dirs)
			{
				DeleteDirectory(dir);
			}

			Directory.Delete(targetDir, false);
		}
	}
}
