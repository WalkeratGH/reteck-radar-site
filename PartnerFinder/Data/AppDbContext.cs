using Microsoft.EntityFrameworkCore;
using PartnerFinder.Models;

namespace PartnerFinder.Data;

// The Entity Framework Core database context. There is a single table (Partners)
// backed by a local SQLite file (partnerfinder.db). See appsettings.json for the
// connection string and Program.cs for automatic migration on startup.
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Partner> Partners => Set<Partner>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Partner>(entity =>
        {
            entity.HasIndex(p => p.CompanyName);
            entity.HasIndex(p => p.Country);
            entity.HasIndex(p => p.RecommendedLevel);
            // Store enums as readable text in SQLite so the raw .db file is easy to inspect.
            entity.Property(p => p.MicrosoftPartnerStatus).HasConversion<string>();
            entity.Property(p => p.DellPartnerStatus).HasConversion<string>();
            entity.Property(p => p.CiscoPartnerStatus).HasConversion<string>();
            entity.Property(p => p.HpePartnerStatus).HasConversion<string>();
            entity.Property(p => p.RecommendedLevel).HasConversion<string>();
            entity.Property(p => p.ManualReviewStatus).HasConversion<string>();
        });
    }
}
