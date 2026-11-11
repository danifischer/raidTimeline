using raidTimeline.Logic.EiJson;
using raidTimeline.Logic.Helper;
using System.Linq;
using System.Web;
using raidTimeline.Models.Models;

namespace raidTimeline.Logic.Mechanics.Strategies
{
	internal class QadimThePeerlessMechanics : BaseMechanics
	{
		public QadimThePeerlessMechanics()
		{
			EncounterIds = new[] { 22000 };
		}

		public override string CreateHtml(RaidModel model)
		{
			var top = "";
			top += @"<table class=""mechanicsTable"" style=""display: none;"">";
			top += @"<tr>
						<th>Player</th>
						<th title=""Pushed by shockwave or dome"">Push</th>
						<th title=""Hit by small or expanding lightning field"">Lightning</th>
						<th title=""Hit by fire field"">Fire</th>
						<th title=""Hit by aimed ball or ball explosion"">Ball</th>
					</tr>";

			foreach (var player in model.Players.OrderByDescending(i => i.CombinedMechanics.Sum(j => j.Value)).Take(3))
			{
				var mid = $@"
					<tr>
						<td>{HttpUtility.HtmlEncode(player.AccountName)}</td>
						<td>{player.CombinedMechanics["prlqadim_push"]}</td>
						<td>{player.CombinedMechanics["prlqadim_lightning"]}</td>
						<td>{player.CombinedMechanics["prlqadim_fire"]}</td>
						<td>{player.CombinedMechanics["prlqadim_ball"]}</td>
					</tr>";
				top += mid;
			}

			top += "</table>";

			return top;
		}

		public override void Parse(EiLog log, PlayerModel playerModel)
		{
			PrepareParsing(log, playerModel);

			var push = playerModel.Mechanics.GetOrDefault("Shck.KB") + playerModel.Mechanics.GetOrDefault("Dome.KB");
			var lightning = playerModel.Mechanics.GetOrDefault("Lght.H") + playerModel.Mechanics.GetOrDefault("S.Lght.H");
			var fire = playerModel.Mechanics.GetOrDefault("Magma.F");
			var ball = playerModel.Mechanics.GetOrDefault("A.Prj.H") + playerModel.Mechanics.GetOrDefault("A.Prj.E");

			playerModel.CombinedMechanics.Add("prlqadim_push", push);
			playerModel.CombinedMechanics.Add("prlqadim_lightning", lightning);
			playerModel.CombinedMechanics.Add("prlqadim_fire", fire);
			playerModel.CombinedMechanics.Add("prlqadim_ball", ball);
		}
	}
}

/* 
 * 0 = Energized Affliction	-> Pylons only
 * 1 = Pushed				-> by Wave			push
 * 2 = Pushed				-> by Dome			push
 * 3 = Street									
 * 4 = Auto Attack (cone)						
 * 5 = Expanding lightning						ligh
 * 6 = Small lightning							ligh
 * 7 = Fire field								fire
 * 8 = Rush										
 * 9 = Aimed Ball								ball
 * 10 = Fixated
 * 11 = Orb caught
 * 12 = Aimed Ball explosion					ball
 * 13 = Thether
 */