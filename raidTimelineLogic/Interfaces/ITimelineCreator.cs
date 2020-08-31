﻿using raidTimelineLogic.Models;
using System.Collections.Generic;

namespace raidTimelineLogic
{
	public interface ITimelineCreator
	{
		void CreateTimelineFileFromDisk(string path, string outputFileName);
		void CreateTimelineFileFromWeb(string path, string outputFileName, string token, int numberOfLogs);
		List<RaidModel> CreateTimelineFileFromWatching(string path, string outputFileName, List<RaidModel> models);
		void BuildHtmlFile(string path, string outputFileName, List<RaidModel> models);
	}
}