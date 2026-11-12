using Microsoft.Extensions.Logging;
using raidTimeline.Logic.DpsReport;
using raidTimeline.Logic.Interfaces;
using raidTimeline.Models.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
        /// Creates a single html file with the timeline and the statistics from the parsed raid models.
        /// The page renders the data in the browser, see <see cref="TimelineDataBuilder"/>.
        /// </summary>
        /// <param name="path">The path where the file should be saved.</param>
        /// <param name="outputFileName">The filename which shall be used.</param>
        /// <param name="models">List of parsed raid models.</param>
        /// <param name="reverse">If 'true' the raid days are listed from newest to oldest, otherwise from oldest to
        /// newest. Default value is 'false'.</param>
        /// <param name="cancellationToken">Cancellation token for stopping the parsing.</param>
        public void BuildTimelineFile(string path, string outputFileName, IEnumerable<RaidModel> models,
            bool reverse = false, CancellationToken cancellationToken = new())
        {
            var modelList = models.ToList();
            _logger?.LogTrace("{ModelCount} logs to build into timeline", modelList.Count);

            if (cancellationToken.IsCancellationRequested) return;

            var json = TimelineDataBuilder.Build(modelList, reverse);
            WriteHtmlFile(Path.Combine(path, outputFileName), json);
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

        private static void WriteHtmlFile(string htmlFilePath, string json)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = assembly.GetManifestResourceNames().Single(name => name.EndsWith("template.html"));

            string html;
            using (var stream = assembly.GetManifestResourceStream(resourceName)!)
            using (var reader = new StreamReader(stream))
            {
                html = reader.ReadToEnd();
            }

            // The json cannot contain "</script>" (see TimelineDataBuilder), so it is safe inside the script tag.
            html = html.Replace("{{data}}", json);

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
    }
}
