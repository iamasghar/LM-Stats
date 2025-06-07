using LM.Stats.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace LM.Stats.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    
    public DbSet<Config> Configs { get; set; }
    public DbSet<Hunt> Hunts { get; set; }
    public DbSet<Kill> Kills { get; set; }
    public DbSet<OtherStat> OtherStats { get; set; }
    public DbSet<StatsInfo> Stats { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // SQL Server specific configuration
        if (Database.IsSqlServer())
        {
            // Configure decimal precision if needed
            modelBuilder.Entity<Kill>(entity =>
            {
                entity.Property(e => e.Might).HasColumnType("decimal(20,0)");
                entity.Property(e => e.OldMight).HasColumnType("decimal(20,0)");
            });
        }

        modelBuilder.Entity<Hunt>()
            .HasOne(h => h.Stats)
            .WithMany(s => s.Hunts)
            .HasForeignKey(h => h.StatsId)
            .OnDelete(DeleteBehavior.Cascade);
            
        modelBuilder.Entity<Kill>()
            .HasOne(k => k.Stats)
            .WithMany(s => s.Kills)
            .HasForeignKey(k => k.StatsId)
            .OnDelete(DeleteBehavior.Cascade);
            
        modelBuilder.Entity<OtherStat>()
            .HasOne(o => o.Stats)
            .WithMany(s => s.OtherStats)
            .HasForeignKey(o => o.StatsId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}