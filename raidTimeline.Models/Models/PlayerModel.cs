using System.Collections.Generic;

namespace raidTimeline.Models.Models
{
	public class PlayerModel
	{
		public int Index { get; init; }
		public int Group { get; init; }
		public long Damage { get; set; }
		public long Dps { get; set; }
		public long Cc { get; set; }
		public double ResTime { get; set; }
		public int ResAmount { get; set; }
		public string AccountName  { get; init; } = default!;
        public string Profession { get; set; } = default!;
        public string ProfessionIcon { get; set; } = default!;
        public bool IsNpc { get; set; }
		public int TimesDowned { get; set; }
		public int TimesDied { get; set; }
		public Dictionary<string, int> Mechanics { get; } = new();
		public Dictionary<string, int> CombinedMechanics { get; } = new();
		public List<BuffModel> Buffs { get; } = new();
	}
}
