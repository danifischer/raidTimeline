namespace raidTimeline.Database.DataModels;

internal class Player
{
    public Guid Id { get; set; }
    public string AccountName { get; set; } = default!;
}