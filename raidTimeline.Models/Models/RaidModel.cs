using System.Globalization;

namespace raidTimeline.Models.Models
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
        public string LogPath { get; set; } = default!;
        public string LogUrl { get; set; } = default!;
        public string EncounterTime { get; set; } = default!;
        public string EncounterName { get; set; } = default!;
        public int EncounterId { get; set; }
        public string EncounterIcon { get; set; } = default!;
        public List<double> HpLeft { get; set; }
        public List<PlayerModel> Players { get; set; }

        public static string DoubleAsHtml(double value) => value.ToString(new CultureInfo("en-US"));
    }
}