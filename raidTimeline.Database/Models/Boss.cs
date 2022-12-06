namespace raidTimeline.Database.Models;

public class Boss
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int FightId { get; set; }
    public string Icon { get; set; } = default!;
    
    public List<Encounter> Encounters { get; set; } = null!;
}

// 3338
// fightId = bossId (17154 - deimos); fightName?