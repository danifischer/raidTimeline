using System.Collections.Generic;
using System.Threading;
using raidTimeline.Models.Models;

namespace raidTimeline.Logic.Interfaces
{
	public interface ITimelineCreator
	{
		/// <summary>
		/// Parse all Elite Insights json files in a defined path.
		/// </summary>
		/// <param name="path">Path which is used to search for json files in.</param>
		/// <param name="cancellationToken">Cancellation token for stopping the parsing.</param>
		/// <returns>List of parsed raid models.</returns>
		IList<RaidModel> ParseFilesFromDisk(string path, CancellationToken cancellationToken = new());
		
		/// <summary>
		/// Parse Elite Insights logs of one day from dps.report.
		/// </summary>
		/// <param name="token">The dps.report token for the user.</param>
		/// <param name="day">The day that shall be parsed.</param>
		/// <param name="cancellationToken">Cancellation token for stopping the parsing.</param>
		/// <returns>List of parsed raid models.</returns>
		IList<RaidModel> ParseFileFromWeb(string token, string day, 
			CancellationToken cancellationToken = new());
		
		/// <summary>
		/// Parse all Elite Insights json files in a defined path, but ignores already parsed ones.
		/// </summary>
		/// <param name="path">Path which is used to search for json files in.</param>
		/// <param name="outputFileName">File name if the summary html file.</param>
		/// <param name="models">List of already parsed raid models.</param>
		/// <param name="cancellationToken">Cancellation token for stopping the parsing.</param>
		/// <returns>List of parsed raid models.</returns>
		IList<RaidModel> ParseFilesFromDiskWhileWatching(string path, string outputFileName, IList<RaidModel> models,
			CancellationToken cancellationToken = new());
		
		/// <summary>
		/// Creates a summary html file from the parsed raid models.
		/// </summary>
		/// <param name="path">The path where the file should be saved.</param>
		/// <param name="outputFileName">The filename which shall be used.</param>
		/// <param name="models">List of parsed raid models.</param>
		/// <param name="reverse">If 'true' the order is from newest to oldest, otherwise from oldest to newest.
		/// Default value is 'false'.</param>
		/// <param name="cancellationToken">Cancellation token for stopping the parsing.</param>
		void BuildTimelineFile(string path, string outputFileName, IEnumerable<RaidModel> models, bool reverse = false,
			CancellationToken cancellationToken = new());
	}
}