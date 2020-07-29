using System;
using System.Collections.Generic;
using System.Globalization;

namespace raidTimelineLogic
{
	internal class RaidModel
	{
		public RaidModel(string filePath, string path)
		{
			LogPath = filePath;
			LogUrl = LogPath.Replace(path, "").Replace("\\", "");

			HpLeft = new List<double>();
		}

		public DateTime OccurenceStart { get; set; }
		public DateTime OccurenceEnd { get; set; }
		public bool Killed { get; set; }
		public string LogPath { get; set; }
		public string LogUrl { get; set; }
		public string EncounterTime { get; set; }
		public string EncounterName { get; set; }
		public string EncounterIcon { get; set; }
		public List<double> HpLeft { get; set; }

		public string DoubleAsHtml(double value) => value.ToString(new CultureInfo("en-US"));
	}
}