using raidTimeline.Database.DataModels;
using raidTimeline.Logic.Models;

namespace raidTimeline.Database.Converters;

internal static class PlayerModelToPlayerConverter
{
    internal static Player Convert(PlayerModel playerModel)
    {
        var player = new Player
        {
            AccountName = playerModel.AccountName
        };
        return player;
    }
}