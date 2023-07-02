using Newtonsoft.Json.Linq;
using raidTimeline.Models.Models;

namespace raidTimeline.Logic.Interfaces
{
	internal interface IMechanics
	{
		string GetEncounterIcon();

		void Parse(JObject logData, PlayerModel playerModel);

		string CreateHtml(RaidModel model);
	}
}