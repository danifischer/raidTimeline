using Microsoft.Extensions.Logging;
using raidTimeline.Logic.DpsReport;
using raidTimeline.Logic.Interfaces;
using raidTimeline.Models.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace raidTimeline.Logic
{
    public class TimelineCreator : ITimelineCreator
    {
        private readonly ILogger _logger;

        public TimelineCreator(ILogger<TimelineCreator> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Parse all Elite Insights json files in a defined path.
        /// </summary>
        /// <param name="path">Path which is used to search for json files in.</param>
        /// <param name="cancellationToken">Cancellation token for stopping the parsing.</param>
        /// <returns>List of parsed raid models.</returns>
        public IList<RaidModel> ParseFilesFromDisk(string path, CancellationToken cancellationToken)
        {
            var models = new List<RaidModel>();

            var parallelOptions = new ParallelOptions
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = 5
            };

            Parallel.ForEach(Directory.GetFiles(path, "*.json"), parallelOptions,
                filePath =>
            {
                _logger?.LogTrace($"Parsing log: {Path.GetFileName(filePath)}");
                var model = EiJsonParser.ParseFile(filePath, _logger);
                if (model != null)
                {
                    // Parallel.ForEach: List<T> is not thread-safe
                    lock (models) models.Add(model);
                }
            });

            return models;
        }

        /// <summary>
        /// Parse all Elite Insights json files in a defined path, but ignores already parsed ones.
        /// </summary>
        /// <param name="path">Path which is used to search for json files in.</param>
        /// <param name="outputFileName">File name if the summary html file.</param>
        /// <param name="models">List of already parsed raid models.</param>
        /// <param name="cancellationToken">Cancellation token for stopping the parsing.</param>
        /// <returns>List of parsed raid models.</returns>
        public IList<RaidModel> ParseFilesFromDiskWhileWatching(string path, string outputFileName,
            IList<RaidModel> models, CancellationToken cancellationToken = new())
        {
            var parallelOptions = new ParallelOptions
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = 5
            };

            var knownFiles = models.Select(i => i.LogPath).ToArray();

            Parallel.ForEach(Directory.GetFiles(path, "*.json"), parallelOptions,
                filePath =>
            {
                if (knownFiles.Contains(filePath)) return;
                
                _logger?.LogTrace($"Parsing log: {Path.GetFileName(filePath)} ");
                var model = EiJsonParser.ParseFile(filePath, _logger);
                if (model != null)
                {
                    _logger?.LogTrace($"Finished: {Path.GetFileName(filePath)}");
                    lock (models) models.Add(model);
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
        /// <param name="cancellationToken">Cancellation token for stopping the parsing.</param>
        public void BuildTimelineFile(string path, string outputFileName, IEnumerable<RaidModel> models,
            bool reverse = false, CancellationToken cancellationToken = new())
        {
            _logger?.LogTrace("{ModelCount} logs to build into timeline", models.Count());
            
            var htmlFileName = outputFileName;
            string htmlFilePath = Path.Combine(path, htmlFileName);


            var sb = new StringBuilder();

            var ordered = reverse
                ? models.OrderByDescending(i => i.OccurenceStart)
                : models.OrderBy(i => i.OccurenceStart);

            foreach (var raidData in ordered.GroupBy(i => i.OccurenceStart.Date))
            {
                CreateHeader(sb, raidData);

                sb.CreateTimelineHeader();
                CreateTimeline(sb, raidData, reverse);
                sb.CreateTimelineFooter();

                if (cancellationToken.IsCancellationRequested) return;
            }

            BuildStatisticsFile(path, models, reverse, cancellationToken);

            if (cancellationToken.IsCancellationRequested) return;
            WriteHtmlFile(htmlFilePath, sb, @"templateTimeline.html");
            _logger?.LogTrace("HTML Magic >>> Done");
        }

        /// <summary>
        /// Parse Elite Insights logs of one day from dps.report.
        /// </summary>
        /// <param name="token">The dps.report token for the user.</param>
        /// <param name="day">The day that shall be parsed.</param>
        /// <param name="cancellationToken">Cancellation token for stopping the parsing.</param>
        /// <returns>List of parsed raid models.</returns>
        public IList<RaidModel> ParseFileFromWeb(string token, string day,
            CancellationToken cancellationToken = new())
        {
            var downloader = new LogDownloader();
            return downloader.DownloadLogsForOneDay(token, day, cancellationToken);
        }

        private static void BuildStatisticsFile(string path, IEnumerable<RaidModel> models,
            bool reverse, CancellationToken cancellationToken)
        {
            const string htmlFileName = "statistics.html";
            string htmlFilePath = Path.Combine(path, htmlFileName);


            var sb = new StringBuilder();

            var ordered = reverse
                ? models.OrderByDescending(i => i.OccurenceStart)
                : models.OrderBy(i => i.OccurenceStart);

            foreach (var raidData in ordered.GroupBy(i => i.OccurenceStart.Date))
            {
                CreateHeader(sb, raidData);
                CreateProfessionTable(sb, raidData, reverse);
                CreateEncounterTable(sb, raidData, reverse);
                CreateGeneralStatsTable(sb, raidData, reverse);

                if (cancellationToken.IsCancellationRequested) return;
            }

            if (cancellationToken.IsCancellationRequested) return;
            WriteHtmlFile(htmlFilePath, sb, @"templateStatistics.html");
        }

        private static void WriteHtmlFile(string htmlFilePath, StringBuilder sb, string template)
        {
            var assembly = Assembly.GetExecutingAssembly();
            string resourceName = assembly.GetManifestResourceNames()
                .Single(str => str.EndsWith(template));
            string html;

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            using (var reader = new StreamReader(stream))
            {
                html = reader.ReadToEnd();
            }

            html = html.Replace("{{placeholder}}", sb.ToString());

            // Write next to the target and move it over, so a timeline that cannot be written completely
            // never replaces a good one (and nothing is written to the current directory).
            var temporaryPath = $"{htmlFilePath}.{Guid.NewGuid():N}.tmp";
            try
            {
                File.WriteAllText(temporaryPath, html);
                File.Move(temporaryPath, htmlFilePath, overwrite: true);
            }
            finally
            {
                if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
            }
        }

        private static void CreateTimeline(StringBuilder sb, IGrouping<DateTime, RaidModel> raidData, bool reverse)
        {
            var ordered = reverse
                ? raidData.OrderByDescending(i => i.OccurenceEnd)
                : raidData.OrderBy(i => i.OccurenceEnd);

            foreach (var model in ordered)
            {
                sb.Append(model.Killed
                    ? HtmlCreator.CreateEncounterHtmlPass(model)
                    : HtmlCreator.CreateEncounterHtmlFail(model));
            }
        }

        private static void CreateEncounterTable(StringBuilder sb, IGrouping<DateTime, RaidModel> raidData, bool reverse)
        {
            var ordered = reverse
                ? raidData.OrderByDescending(i => i.OccurenceEnd)
                : raidData.OrderBy(i => i.OccurenceEnd);

            var players = ordered.SelectMany(i => i.Players).Where(j => !j.IsNpc)
                .Select(j => j.AccountName)
                .Distinct().OrderBy(name => name).ToArray();

            sb.Append(HtmlCreator.CreateEncounterTableHeader(players));

            foreach (var model in ordered)
            {
                var tableRow = new EncounterTableModel(model, players);
                sb.Append(HtmlCreator.CreateEncounterTableEntry(tableRow));
            }

            sb.Append(HtmlCreator.CreateEncounterTableFooter());
        }
        
        private static void CreateGeneralStatsTable(StringBuilder sb, IGrouping<DateTime, RaidModel> raidData, bool reverse)
        {
            var ordered = reverse
                ? raidData.OrderByDescending(i => i.OccurenceEnd)
                : raidData.OrderBy(i => i.OccurenceEnd);

            var players = ordered.SelectMany(i => i.Players).Where(j => !j.IsNpc)
                .Select(j => j.AccountName)
                .Distinct().OrderBy(name => name).ToArray();

            var tableRows = new List<GeneralStatsTableModel>();

            sb.Append(HtmlCreator.CreateGeneralStatsTableHeader(players));

            foreach (var model in ordered)
            {
                var tableRow = new GeneralStatsTableModel(model, players);
                tableRows.Add(tableRow);
                sb.Append(HtmlCreator.CreateGeneralStatsTableEntry(tableRow));
            }

            sb.Append(HtmlCreator.CreateGeneralStatsTableFooter(tableRows, players));

        }

        private static void CreateProfessionTable(StringBuilder sb, IGrouping<DateTime, RaidModel> raidData, bool reverse)
        {
            var ordered = reverse
                ? raidData.OrderByDescending(i => i.OccurenceEnd)
                : raidData.OrderBy(i => i.OccurenceEnd);

            var tryTime = new TimeSpan(raidData.Select(i => i.OccurenceEnd - i.OccurenceStart)
                .Sum(i => i.Ticks));

            sb.Append(HtmlCreator.CreateOverviewTableHeader());

            foreach (var group in ordered.GroupBy(i => i.EncounterName))
            {
                var tableRow = new OverviewTableModel(group.ToArray(), tryTime);
                sb.Append(HtmlCreator.CreateOverviewTableEntry(tableRow));
            }

            sb.Append(HtmlCreator.CreateOverviewTableFooter());
        }

        private static void CreateHeader(StringBuilder sb, IGrouping<DateTime, RaidModel> raidData)
        {
            var killed = raidData.Count(i => i.Killed);
            var failed = raidData.Count(i => !i.Killed);
            var bosses = raidData.Select(i => i.EncounterName).Distinct().Count();

            var tryTime = new TimeSpan(raidData.Select(i => i.OccurenceEnd - i.OccurenceStart)
                .Sum(i => i.Ticks));
            var raidTime = raidData.Max(i => i.OccurenceEnd) - raidData.Min(i => i.OccurenceStart);

            sb.Append(HtmlCreator.CreateHeaderHtml(raidData.Key, killed, failed, tryTime, raidTime, bosses));
        }
    }
}