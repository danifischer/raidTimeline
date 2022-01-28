using System.Text;
using raidTimelineLogic.Mechanics;
using raidTimelineLogic.Models;

namespace raidTimelineLogic.HtmlBuilders
{
    public static class MechanicsTableBuilder
    {
        public static StringBuilder BuildMechanicsTable(this StringBuilder stringBuilder, RaidModel raidModel)
        {
            var strategy = MechanicsFactory
                .GetMechanicsFactory()
                .FindStrategy(raidModel.EncounterIcon);

            if (strategy != null)
            {
                stringBuilder.Append(strategy.CreateHtml(raidModel));
            }

            return stringBuilder;
        }
    }
}