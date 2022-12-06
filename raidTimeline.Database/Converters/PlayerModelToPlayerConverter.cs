using raidTimeline.Database.Models;
using raidTimeline.Logic.Models;

namespace raidTimeline.Database.Converters;

internal static class PlayerModelToPlayerConverter
{
    internal static Player ConvertPlayer(PlayerModel playerModel)
    {
        var player = new Player
        {
            AccountName = playerModel.AccountName
        };
        return player;
    }
}