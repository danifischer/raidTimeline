using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.IO;
using System.Linq;
using raidTimelineLogic;
using FluentAssertions;

namespace raidTimelineLogicTests
{
	[TestClass]
	public class UnitTest1
	{
		[TestMethod]
		public void TestWithLatestEi()
		{
			// Prepare
			var pathToEi = @"D:\git\gw2\GW2-Elite-Insights-Parser\GW2EI.bin\Debug\GuildWars2EliteInsights.exe";
			var testFile = Path.Combine(Directory.GetCurrentDirectory(), "20200801-211921.zevtc");
			ProcessStartInfo psi = new ProcessStartInfo
			{
				FileName = pathToEi,
				Arguments = $"-p {testFile}",
				UseShellExecute = true
			};
			Process.Start(psi);
			var htmlFile = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.html");
			var parser = new EiHtmlParser();

			// Test
			var log = parser.ParseLog(htmlFile.First());

			// Check
			log.Should().NotBeNull();
		}
	}
}
