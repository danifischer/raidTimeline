using System.Collections.Generic;

namespace raidTimeline.Logic.Models
{
	public class PlayerModel
	{
		public int Index { get; init; }
		public long Damage { get; set; }
		public long Dps { get; set; }
		public long Cc { get; set; }
		public double ResTime { get; set; }
		public int ResAmount { get; set; }
		public string AccountName  { get; init; }
		public string Profession { get; set; }
		public string ProfessionIcon { get; set; }
		public bool IsNpc { get; set; }
		public Dictionary<string, int> Mechanics { get; } = new();
		public Dictionary<string, int> CombinedMechanics { get; } = new();
		public List<BuffModel> Buffs { get; } = new();
	}
}
