using System.Collections.Generic;

namespace raidTimelineLogic.Models
{
	public class PlayerModel
	{
		public int Index { get; set; }
		public long Damage { get; set; }
		public long Dps { get; set; }
		public long Cc { get; set; }
		public double ResTime { get; set; }
		public int ResAmmount { get; set; }
		public string AccountName  { get; set; }
		public Dictionary<string, int> Mechanics { get; set; } = new Dictionary<string, int>();
		public Dictionary<string, int> CombinedMechanics { get; set; } = new Dictionary<string, int>();
		public List<BuffModel> Buffs { get; set; } = new List<BuffModel>();
	}
}
