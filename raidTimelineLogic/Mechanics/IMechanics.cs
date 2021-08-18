using raidTimelineLogic.Models;

namespace raidTimelineLogic.Mechanics
{
	internal interface IMechanics
	{
		string GetEncounterIcon();

		void Parse(dynamic logData, PlayerModel playerModel);

		string CreateHtml(RaidModel model);
	}
}