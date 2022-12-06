using FluentAssertions;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using Xunit;

namespace raidTimeline.Logic.Tests
{
	public class IntegrationTests
	{
		private static readonly string TestFile = Path.Combine(Directory.GetCurrentDirectory(), @"Files\test_log.zevtc");

		[Fact]
		public void ParseLog_LatestRelease_ShouldParse()
		{
			// Prepare
			if(Directory.Exists("temp"))
			{
				DeleteDirectory("temp");
			}
			Directory.CreateDirectory("temp");
			string x = GetDownloadPathForLatestVersion();
			DownloadLatestVersion(x);

			var pathToEi = Path.Combine(Directory.GetCurrentDirectory(), @"temp\GuildWars2EliteInsights.exe");
			var htmlFile = CreateHtmlLog(pathToEi);
			
			// Test
			var log = Logic.EiHtmlParser.ParseLog(htmlFile);

			// Check
			log.Should().NotBeNull();

			// Cleanup
			File.Delete(htmlFile);
			DeleteDirectory("temp");
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

		private static void DownloadLatestVersion(string x)
		{
			var client = new HttpClient();
			var response = client.GetAsync(x).Result;
			using (var fileStream = new FileStream(@"temp\version.zip", FileMode.Create))
			{
				response.Content.CopyToAsync(fileStream).Wait();
			}
			ZipFile.ExtractToDirectory(@"temp\version.zip", "temp");
		}

		private static string GetDownloadPathForLatestVersion()
		{
			var client = new RestClient("https://api.github.com/");
			var request = new RestRequest("repos/baaron4/GW2-Elite-Insights-Parser/releases/latest", DataFormat.Json);
			var response = client.Get(request);
			var content = response.Content;
			var json = (dynamic)JsonConvert.DeserializeObject(content);

			foreach (var assert in json.assets)
			{
				if (assert.name == "GW2EI.zip")
				{
					return assert.browser_download_url;
				}
			}

			throw new Exception("Latest version could not be found.");
		}

		// This is needed as Directory.Delete is stupid ...
		private void DeleteDirectory(string targetDir)
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