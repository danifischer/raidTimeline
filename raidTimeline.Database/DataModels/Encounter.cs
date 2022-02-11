namespace raidTimeline.Database.DataModels;

internal class Encounter
{
    public Guid Id { get; set; }
    
    public DateTime OccurenceStart { get; set; }
    public DateTime OccurenceEnd { get; set; }
    public bool Killed { get; set; }
    public string EncounterTime { get; set; } = null!;
    public double HitPointsRemaining { get; set; }
    
    public Boss Boss { get; set; } = null!;
    public List<Player> Players { get; set; } = null!;
}