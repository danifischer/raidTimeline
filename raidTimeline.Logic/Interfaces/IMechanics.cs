using raidTimeline.Logic.Models;

namespace raidTimeline.Logic.Interfaces
{
	internal interface IMechanics
	{
		string GetEncounterIcon();

		void Parse(dynamic logData, PlayerModel playerModel);

		string CreateHtml(RaidModel model);
	}
}