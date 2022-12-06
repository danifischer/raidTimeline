namespace raidTimeline.Database.Models;

public class Player
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string AccountName { get; set; } = default!;
    
    public List<Encounter> Encounters { get; set; } = null!;
}