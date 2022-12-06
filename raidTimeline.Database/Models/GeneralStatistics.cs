namespace raidTimeline.Database.Models;

public class GeneralStatistics
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Encounter Encounter { get; set; } = null!;
    public Player Player { get; set; } = null!;
    
    public long Damage { get; set; }
    public long Dps { get; set; }
    public long Cc { get; set; }
    public double ResTime { get; set; }
    public int ResAmount { get; set; }
}