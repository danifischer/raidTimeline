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

        /// <summary>Columns of the boss mechanics table, empty if nothing is worth showing.</summary>
        public List<MechanicColumn> MechanicColumns { get; } = new();

        public static string DoubleAsHtml(double value) => value.ToString(new CultureInfo("en-US"));
    }
}