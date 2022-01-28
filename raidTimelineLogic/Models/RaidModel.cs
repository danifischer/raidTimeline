using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace raidTimelineLogic.Models
{
	public class RaidModel
	{
		public RaidModel(string filePath)
		{
			LogPath = filePath;
			LogUrl = Path.GetFileName(filePath);
			HpLeft = new List<double>();
			Players = new List<PlayerModel>();
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
		public List<PlayerModel> Players { get; set; }

		public static string DoubleAsHtml(double value) => value.ToString(new CultureInfo("en-US"));
	}
}