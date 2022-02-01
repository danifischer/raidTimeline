using raidTimelineLogic.Models;

namespace raidTimelineLogic.Interfaces
{
	internal interface IMechanics
	{
		string GetEncounterIcon();

		void Parse(dynamic logData, PlayerModel playerModel);

		string CreateHtml(RaidModel model);
	}
}