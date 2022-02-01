using Newtonsoft.Json;
using raidTimelineLogic.Models;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using raidTimelineLogic.Interfaces;

namespace raidTimelineLogic
{
	public class TimelineCreator : ITimelineCreator
	{
		/// <summary>
		/// Parse all elite insights html files in a defined path.
		/// </summary>
		/// <param name="path">Path which is used to search for html files in.</param>
		/// <returns>List of parsed raid models.</returns>
		public IList<RaidModel> ParseFilesFromDisk(string path)
		{
			var models = new List<RaidModel>();

			Parallel.ForEach(Directory.GetFiles(path, "*.html"), filePath =>
			{
				Console.WriteLine($"Parsing log: {Path.GetFileName(filePath)}");
				var model = EiHtmlParser.ParseLog(filePath);
				if (model != null)
					models.Add(model);
			});
			
			return models;
		}

		/// <summary>
		/// Parse all elite insights html files in a defined path, but ignores already parsed ones.
		/// </summary>
		/// <param name="path">Path which is used to search for html files in.</param>
		/// <param name="outputFileName">File name if the summary html file.</param>
		/// <param name="models">List of already parsed raid models.</param>
		/// <returns>List of parsed raid models.</returns>
		public IList<RaidModel> ParseFilesFromDiskWhileWatching(string path, string outputFileName, 
			IList<RaidModel> models)
		{
			var knownFiles = models.Select(i => i.LogPath).ToArray();

			Parallel.ForEach(Directory.GetFiles(path, "*.html"), filePath =>
			{
				if (knownFiles.Contains(filePath)) return;
				if (filePath.EndsWith(outputFileName)) return;

				Console.WriteLine($"Parsing log: {Path.GetFileName(filePath)} ");
				var model = EiHtmlParser.ParseLog(filePath);
				if (model != null)
				{
					Console.WriteLine($"Finished: {Path.GetFileName(filePath)}");
					models.Add(model);
				}
			});

			return models;
		}

		/// <summary>
		/// Creates a summary html file from the parsed raid models.
		/// </summary>
		/// <param name="path">The path where the file should be saved.</param>
		/// <param name="outputFileName">The filename which shall be used.</param>
		/// <param name="models">List of parsed raid models.</param>
		/// <param name="reverse">If 'true' the order is from newest to oldest, otherwise from oldest to newest.
		/// Default value is 'false'.</param>
		public void BuildTimelineFile(string path, string outputFileName, IEnumerable<RaidModel> models, bool reverse = false)
		{
			var htmlFileName = outputFileName;
			string htmlFilePath = Path.Combine(path, htmlFileName);

			if (File.Exists(htmlFilePath))
			{
				File.Delete(Path.Combine(htmlFilePath));
			}

			var sb = new StringBuilder();

			var ordered = reverse
				? models.OrderByDescending(i => i.OccurenceStart)
				: models.OrderBy(i => i.OccurenceStart);

			foreach (var raidDate in ordered.GroupBy(i => i.OccurenceStart.Date))
			{
				CreateHeader(sb, raidDate);
				CreateTimeline(sb, raidDate, reverse);
				sb.Append("</div>");
			}

			Console.Write("HTML Magic ");
			WriteHtmlFile(htmlFileName, htmlFilePath, sb);
			Console.WriteLine(">>> Done");
		}

		/// <summary>
		/// Parse elite insights html files from dps.report.
		/// </summary>
		/// <param name="path">The path which shall be used to temporarily store logs.</param>
		/// <param name="token">The dps.report token for the user.</param>
		/// <param name="numberOfLogs">The number of logs which shall be parsed.</param>
		/// <returns>List of parsed raid models.</returns>
		public IList<RaidModel> ParseFileFromWeb(string path, string token, int numberOfLogs)
		{
			var models = new List<RaidModel>();

			var page = 1;
			var filePath = Path.Combine(path, "test.html");
			var httpClient = new HttpClient();

			while (numberOfLogs > 0)
			{
				var client = new RestClient("https://dps.report/");
				var request = new RestRequest($"getUploads?userToken={token}&page={page++}", DataFormat.Json);
				var response = client.Get(request);

				var content = response.Content;
				var json = (dynamic)JsonConvert.DeserializeObject(content);

				if (json == null) continue;

				foreach (var upload in json.uploads)
				{
					if (numberOfLogs <= 0) break;

					Console.WriteLine($"Loading log {upload.permalink.Value}");
					Task<string> getFileTask = httpClient.GetStringAsync(upload.permalink.Value);
					var html = getFileTask.Result;

					html = html.Replace("/cache/", "https://dps.report/cache/");
					File.WriteAllText(filePath, html);

					Console.WriteLine($"Parsing log {upload.permalink.Value}");
					var model = EiHtmlParser.ParseLog(filePath);
					model.LogUrl = upload.permalink.Value;
					models.Add(model);

					numberOfLogs--;
				}

				if (page > json.pages.Value) break;
			}

			File.Delete(filePath);
			return models;
		}

		private static void WriteHtmlFile(string htmlFileName, string htmlFilePath, StringBuilder sb)
		{
			var html = File.ReadAllText(@"template.html");
			html = html.Replace("{{placeholder}}", sb.ToString());
			File.WriteAllText(htmlFileName, html);
			File.Move(htmlFileName, htmlFilePath);
		}

		private static void CreateTimeline(StringBuilder sb, IGrouping<DateTime, RaidModel> raidDate, bool reverse)
		{
			var ordered = reverse
				? raidDate.OrderByDescending(i => i.OccurenceEnd)
				: raidDate.OrderBy(i => i.OccurenceEnd);

			foreach (var model in ordered)
			{
				sb.Append(model.Killed
					? HtmlCreator.CreateEncounterHtmlPass(model)
					: HtmlCreator.CreateEncounterHtmlFail(model));
			}
		}

		private static void CreateHeader(StringBuilder sb, IGrouping<DateTime, RaidModel> raidDate)
		{
			var killed = raidDate.Count(i => i.Killed);
			var failed = raidDate.Count(i => !i.Killed);
			var bosses = raidDate.Select(i => i.EncounterName).Distinct().Count();

			var tryTime = new TimeSpan(raidDate.Select(i => i.OccurenceEnd - i.OccurenceStart)
				.Sum(i => i.Ticks));
			var raidTime = raidDate.Max(i => i.OccurenceEnd) - raidDate.Min(i => i.OccurenceStart);

			sb.Append(HtmlCreator.CreateHeaderHtml(raidDate.Key, killed, failed, tryTime, raidTime, bosses));
		}
	}
}