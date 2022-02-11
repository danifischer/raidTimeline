using raidTimeline.Logic.Helper;
using System.Linq;
using System.Web;
using raidTimeline.Logic.Models;

namespace raidTimeline.Logic.Mechanics.Strategies
{
	internal class QadimMechanics : BaseMechanics
	{
		public QadimMechanics()
		{
			EncounterIcon = "https://wiki.guildwars2.com/images/f/f2/Mini_Qadim.png";
		}

		public override string CreateHtml(RaidModel model)
		{
			var top = "";
			top += @"<table class=""mechanicsTable"" style=""display: none;"">";
			top += @"<tr>
						<th>Player</th>
						<th title=""Wyvern attacks (Slash, Tail & Fire Breath)"">Wyv</th>
						<th title=""Destroyer attacks (Wave, Slam & Claw)"">Dest</th>
						<th title=""Qadim attacks (Flame Dance, Flame Wave, Fire Wave, Inferno & Sea of Flames)"">Qad</th>
						<th title=""Swap (Ported from below Legendary Creature to Qadim)"">Port</th>
					</tr>";

			foreach (var player in model.Players.OrderByDescending(i => i.CombinedMechanics.Sum(j => j.Value)).Take(3))
			{
				var mid = $@"
					<tr>
						<td>{HttpUtility.HtmlEncode(player.AccountName)}</td>
						<td>{player.CombinedMechanics["qadim_wvyern"]}</td>
						<td>{player.CombinedMechanics["qadim_destroyer"]}</td>
						<td>{player.CombinedMechanics["qadim_qadim"]}</td>
						<td>{player.CombinedMechanics["qadim_port"]}</td>
					</tr>";
				top += mid;
			}

			top += "</table>";

			return top;
		}

		public override void Parse(dynamic logData, PlayerModel playerModel)
		{
			PrepareParsing(logData, playerModel);

			var wyvern = playerModel.Mechanics.GetOrDefault("Slash") + playerModel.Mechanics.GetOrDefault("W.Pizza") + playerModel.Mechanics.GetOrDefault("W.Breath");
			var destroyer = playerModel.Mechanics.GetOrDefault("D.Wave") + playerModel.Mechanics.GetOrDefault("D.Slam") + playerModel.Mechanics.GetOrDefault("Claw");
			var qadim = playerModel.Mechanics.GetOrDefault("F.Dance") + playerModel.Mechanics.GetOrDefault("KB") + playerModel.Mechanics.GetOrDefault("Q.Wave") 
				+ playerModel.Mechanics.GetOrDefault("Inf") + playerModel.Mechanics.GetOrDefault("Q.Hitbox");
			var port = playerModel.Mechanics.GetOrDefault("Port");

			playerModel.CombinedMechanics.Add("qadim_wvyern", wyvern);
			playerModel.CombinedMechanics.Add("qadim_destroyer", destroyer);
			playerModel.CombinedMechanics.Add("qadim_qadim", qadim);
			playerModel.CombinedMechanics.Add("qadim_port", port);
		}
	}
}

/* 
 * 0 = Flame Dance	-> Fire on platform		Qad
 * 1 = Flame Wave	-> KB					Qad
 * 2 = Fire Wave	-> Big Bad Mace attack	Qad
 * 3 = Fire Wave	-> Destroyer Wave		Dest
 * 4 = Inferno		-> Lava pool			Qad
 * 5 = Slash		-> Wyvern double att.	Wyv
 * 6 = Tail			-> Wyvern tail att.		Wyv
 * 7 = Fire Breath	-> Wyvern fire att.		Wyv
 * 8 = Shattered	-> Destroyer slam		Dest
 * 9 = Sea of Flame	-> Quadim hitbox		Qad
 * 10 = Claw		-> Reaper att.			Dest
 * 11 = Port		-> All creatures		Port
 * 
 * Wyv
 * Dest
 * Qad
 * Port
 */