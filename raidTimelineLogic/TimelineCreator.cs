using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace raidTimelineLogic
{
	public class TimelineCreator : ITimelineCreator
	{
		public void CreateTimelineFile(string path, string outputFileName)
		{
			var htmlFileName = outputFileName;
			string htmlFilePath = Path.Combine(path, htmlFileName);
			var parser = new EiHtmlParser();

			var models = new List<RaidModel>();

			if (File.Exists(htmlFilePath))
			{
				File.Delete(Path.Combine(htmlFilePath));
			}

			foreach (var file in Directory.GetFiles(path))
			{
				Console.WriteLine("Parsing log ...");
				var model = parser.ParseLog(path, file);
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
