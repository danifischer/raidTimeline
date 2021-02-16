using raidTimelineLogic.Models;
using System.Collections.Generic;

namespace raidTimelineLogic
{
	public interface ITimelineCreator
	{
		void CreateTimelineFileFromDisk(string path, string outputFileName, bool reverse = false);
		void CreateTimelineFileFromWeb(string path, string outputFileName, string token, int numberOfLogs, bool reverse = false);
		List<RaidModel> CreateTimelineFileFromWatching(string path, string outputFileName, List<RaidModel> models, bool reverse = false);
		void BuildHtmlFile(string path, string outputFileName, List<RaidModel> models, bool reverse = false);
	}
}