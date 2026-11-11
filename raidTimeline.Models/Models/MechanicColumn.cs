namespace raidTimeline.Models.Models
{
    /// <summary>
    /// One column of the boss mechanics table of an encounter. The value of the column for a player
    /// is stored in <see cref="PlayerModel.CombinedMechanics"/> under <see cref="Key"/>.
    /// </summary>
    /// <param name="Key">Key in <see cref="PlayerModel.CombinedMechanics"/>.</param>
    /// <param name="Label">Short column header.</param>
    /// <param name="Title">Tooltip of the column header.</param>
    /// <param name="Rank">Whether the column counts when the players with the most mechanic fails are chosen.</param>
    public record MechanicColumn(string Key, string Label, string Title, bool Rank = true);
}
