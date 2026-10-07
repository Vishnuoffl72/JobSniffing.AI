using Microsoft.EntityFrameworkCore;
using PulseJob.Api.Models;

namespace PulseJob.Api.Data;

public class PulseJobDbContext : DbContext
{
    public PulseJobDbContext(DbContextOptions<PulseJobDbContext> options) : base(options)
    {
    }

    public DbSet<JobSearchResult> JobSearchResults => Set<JobSearchResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<JobSearchResult>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ScrapeTimestamp);
            entity.HasIndex(e => e.Platform);
            entity.HasIndex(e => e.MatchScore);
        });
    }
}

