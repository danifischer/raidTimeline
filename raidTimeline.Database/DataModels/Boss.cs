namespace raidTimeline.Database.DataModels;

internal class Boss
{
    public Guid Id { get; set; }
    public int FightId { get; set; }
    public string Icon { get; set; } = default!;
}

// 3338
// fightId = bossId (17154 - deimos); fightName?