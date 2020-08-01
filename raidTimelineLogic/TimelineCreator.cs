using Newtonsoft.Json;
using raidTimelineLogic.Models;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace raidTimelineLogic
{
	public class TimelineCreator : ITimelineCreator
	{
		public void CreateTimelineFileFromDisk(string path, string outputFileName)
		{
			var htmlFileName = outputFileName;
			string htmlFilePath = Path.Combine(path, htmlFileName);
			var parser = new EiHtmlParser();

			var models = new List<RaidModel>();

			if (File.Exists(htmlFilePath))
			{
				File.Delete(Path.Combine(htmlFilePath));
			}

			foreach (var filePath in Directory.GetFiles(path, "*.html"))
			{
				Console.WriteLine($"Parsing log: {Path.GetFileName(filePath)}");
				var model = parser.ParseLog(filePath);
				if(model != null)
					models.Add(model);
			}

			StringBuilder sb = new StringBuilder();

			foreach (var raidDate in models.GroupBy(i => i.OccurenceStart.Date))
			{
				CreateHeader(sb, raidDate);
				CreateTimeline(sb, raidDate);
				sb.Append("</div>");
			}

			Console.WriteLine("HTML Magic ...");
			WriteHtmlFile(htmlFileName, htmlFilePath, sb);
		}

		public void CreateTimelineFileFromWeb(string path, string outputFileName, string token, int numberOfLogs)
		{
			var htmlFileName = outputFileName;
			string htmlFilePath = Path.Combine(path, htmlFileName);
			var parser = new EiHtmlParser();

			var models = new List<RaidModel>();

			if (File.Exists(htmlFilePath))
			{
				File.Delete(Path.Combine(htmlFilePath));
			}

			var page = 1;
			var filePath = Path.Combine(path, "test.html");
			var wc = new System.Net.WebClient();

			while (numberOfLogs > 0)
			{
				var client = new RestClient("https://dps.report/");
				var request = new RestRequest($"getUploads?userToken={token}&page={page++}", DataFormat.Json);
				var response = client.Get(request);

				var content = response.Content;
				var json = (dynamic)JsonConvert.DeserializeObject(content);

				foreach (var upload in json.uploads)
				{
					if (numberOfLogs <= 0) break;

					Console.WriteLine($"Loading log {upload.permalink.Value}");
					wc.DownloadFile(upload.permalink.Value, filePath);

					var html = File.ReadAllText(filePath);
					html = html.Replace("/cache/", "https://dps.report/cache/");
					File.WriteAllText(filePath, html);

					Console.WriteLine($"Parsing log {upload.permalink.Value}");
					var model = parser.ParseLog(filePath);
					model.LogUrl = upload.permalink.Value;
					if (model != null)
						models.Add(model);

					numberOfLogs--;
				}

				if (page > json.pages.Value) break;
			}

			File.Delete(filePath);
			StringBuilder sb = new StringBuilder();

			foreach (var raidDate in models.GroupBy(i => i.OccurenceStart.Date))
			{
				CreateHeader(sb, raidDate);
				CreateTimeline(sb, raidDate);
				sb.Append("</div>");
			}

			Console.WriteLine("HTML Magic ...");
			WriteHtmlFile(htmlFileName, htmlFilePath, sb);
		}

		private static void WriteHtmlFile(string htmlFileName, string htmlFilePath, StringBuilder sb)
		{
			var html = File.ReadAllText(@"template.html");
			html = html.Replace("{{placeholder}}", sb.ToString());
			File.WriteAllText(htmlFileName, html);
			File.Move(htmlFileName, htmlFilePath);
		}

		private static void CreateTimeline(StringBuilder sb, IGrouping<DateTime, RaidModel> raidDate)
		{
			foreach (var model in raidDate.OrderBy(i => i.OccurenceEnd))
			{
				if (model.Killed)
					sb.Append(HtmlCreator.CreateEncounterHtmlPass(model));
				else
					sb.Append(HtmlCreator.CreateEncounterHtmlFail(model));
			}
		}

		private static void CreateHeader(StringBuilder sb, IGrouping<DateTime, RaidModel> raidDate)
		{
			var killed = raidDate.Count(i => i.Killed);
			var failed = raidDate.Count(i => !i.Killed);
			var bosses = raidDate.Select(i => i.EncounterName).Distinct().Count();

			var tryTime = new TimeSpan(raidDate.Select(i => i.OccurenceEnd - i.OccurenceStart).Sum(i => i.Ticks));
			var raidTime = raidDate.Max(i => i.OccurenceEnd) - raidDate.Min(i => i.OccurenceStart);

			sb.Append(HtmlCreator.CreateHeaderHtml(raidDate.Key, killed, failed, tryTime, raidTime, bosses));
		}
	}
}