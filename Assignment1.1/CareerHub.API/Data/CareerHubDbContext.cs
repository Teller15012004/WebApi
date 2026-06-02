using CareerHub.API.Models;
using Microsoft.EntityFrameworkCore;

namespace CareerHub.API.Data;

// Assignment 2.1 — Bridge between C# and PostgreSQL
// Owns the connection, change tracker, and table access
public class CareerHubDbContext : DbContext
{
    public CareerHubDbContext(DbContextOptions<CareerHubDbContext> options)
        : base(options)
    {
    }

    // Represents the job_listings table
    public DbSet<JobListing> JobListings => Set<JobListing>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<JobListing>(entity =>
        {
            // Lowercase table name — PostgreSQL convention
            entity.ToTable("job_listings");

            entity.HasKey(e => e.Id);

            // We supply the Guid — database does not generate it
            entity.Property(e => e.Id)
                  .ValueGeneratedNever();

            // String constraints at database level — defence in depth
            entity.Property(e => e.Title)
                  .IsRequired()
                  .HasMaxLength(120);

            entity.Property(e => e.Company)
                  .IsRequired()
                  .HasMaxLength(80);

            entity.Property(e => e.Location)
                  .IsRequired();

            entity.Property(e => e.Description)
                  .IsRequired();

            // Unique index — database-level duplicate prevention
            // Backs up DuplicateJobListingException in the application layer
            entity.HasIndex(e => new { e.Title, e.Company })
                  .IsUnique();
        });
    }
}