using Microsoft.EntityFrameworkCore;
using raidTimeline.Database.Models;

namespace raidTimeline.Database.Context;

public sealed class RaidContext : DbContext
{
    public RaidContext()
    {
        Database.Migrate();
    }
    
    internal RaidContext(DbContextOptions<DbContext> options) : base(options)
    {
        Database.Migrate();
    }
    
    internal DbSet<Boss> Bosses { get; set; } = null!;
    internal DbSet<Encounter> Encounters { get; set; } = null!;
    internal DbSet<GeneralStatistics> GeneralStatistics { get; set; } = null!;
    internal DbSet<Player> Players { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseNpgsql("Host=127.0.0.1;Database=db;Username=postgres;Password=postgres");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Boss>()
            .ToTable("Bosses")
            .HasMany(b => b.Encounters)
            .WithOne();

        modelBuilder.Entity<GeneralStatistics>()
            .ToTable("GeneralStatistics");
        
        modelBuilder.Entity<Player>()
            .ToTable("Players");

        modelBuilder.Entity<Encounter>()
            .Property<Guid>("BossForeignKey");

        modelBuilder.Entity<Encounter>()
            .ToTable("Encounters")
            .HasMany(e => e.Players)
            .WithMany(p => p.Encounters)
            .UsingEntity(j => j.ToTable("EncounterPlayer"))
            .HasOne(b => b.Boss)
            .WithMany(e => e.Encounters)
            .HasForeignKey("BossForeignKey");

    }
}