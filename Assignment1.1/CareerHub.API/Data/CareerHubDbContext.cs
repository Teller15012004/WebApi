using CareerHub.API.Models;
using Microsoft.EntityFrameworkCore;

namespace CareerHub.API.Data;

// Assignment 2.1 — Original DbContext
// Assignment 2.2 — Added Company, Applicant, Application entities
//                  Configured all relationships using Fluent API
//                  Added N+1 query logging temporarily for diagnosis
public class CareerHubDbContext : DbContext
{
    public CareerHubDbContext(DbContextOptions<CareerHubDbContext> options)
        : base(options)
    {
    }

    // Assignment 2.1 — Job listings table
    public DbSet<JobListing> JobListings => Set<JobListing>();

    // Assignment 2.2 — New tables
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Applicant> Applicants => Set<Applicant>();
    public DbSet<Application> Applications => Set<Application>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ── Company ───────────────────────────────────────────────────────
        modelBuilder.Entity<Company>(entity =>
        {
            // Lowercase table name — PostgreSQL convention
            entity.ToTable("companies");

            entity.HasKey(e => e.Id);

            // We supply the Guid — database does not generate it
            entity.Property(e => e.Id)
                  .ValueGeneratedNever();

            entity.Property(e => e.Name)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(e => e.Website)
                  .HasMaxLength(200);

            entity.Property(e => e.Industry)
                  .HasMaxLength(100);

            // Unique company names — no duplicates
            entity.HasIndex(e => e.Name)
                  .IsUnique();
        });

        // ── JobListing ────────────────────────────────────────────────────
        modelBuilder.Entity<JobListing>(entity =>
        {
            entity.ToTable("job_listings");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                  .ValueGeneratedNever();

            entity.Property(e => e.Title)
                  .IsRequired()
                  .HasMaxLength(120);

            entity.Property(e => e.Location)
                  .IsRequired();

            entity.Property(e => e.Description)
                  .IsRequired();

            // Assignment 2.2 — Configure Company → JobListing relationship
            // One Company has many JobListings
            // A JobListing belongs to one Company via CompanyId foreign key
            // Restrict = block deletion of a company that has listings
            // This protects applicant data from being silently wiped
            entity.HasOne(j => j.Company)
                  .WithMany(c => c.JobListings)
                  .HasForeignKey(j => j.CompanyId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Unique index on Title + Company combination
            // Same rule enforced at application layer by DuplicateJobListingException
            entity.HasIndex(e => new { e.Title, e.CompanyId })
                  .IsUnique();
        });

        // ── Applicant ─────────────────────────────────────────────────────
        modelBuilder.Entity<Applicant>(entity =>
        {
            entity.ToTable("applicants");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                  .ValueGeneratedNever();

            entity.Property(e => e.FullName)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(e => e.Email)
                  .IsRequired()
                  .HasMaxLength(150);

            // Each email address is unique — one account per email
            entity.HasIndex(e => e.Email)
                  .IsUnique();
        });

        // ── Application (join entity) ─────────────────────────────────────
        modelBuilder.Entity<Application>(entity =>
        {
            entity.ToTable("applications");

            // Composite primary key — (JobListingId, ApplicantId)
            // Enforces one application per applicant per listing at DB level
            // A generated Guid would allow duplicate applications
            entity.HasKey(e => new { e.JobListingId, e.ApplicantId });

            // Application → JobListing relationship
            // Cascade = if a listing is deleted, remove its applications too
            // An application cannot exist without its listing
            entity.HasOne(a => a.JobListing)
                  .WithMany(j => j.Applications)
                  .HasForeignKey(a => a.JobListingId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Application → Applicant relationship
            // Cascade = if an applicant is deleted, remove their applications
            entity.HasOne(a => a.Applicant)
                  .WithMany(ap => ap.Applications)
                  .HasForeignKey(a => a.ApplicantId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }

    // Assignment 2.2 — TEMPORARY — remove before committing
// Logs every SQL query to the terminal so you can count them
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    if (!optionsBuilder.IsConfigured)
        return;

    optionsBuilder.LogTo(Console.WriteLine, LogLevel.Information);
}
      // ── JobListing constraints ─────────────────────────────────────────────
      modelBuilder.Entity<JobListing>(entity =>
{
    // ... your existing configuration stays here ...

    // CHECK CONSTRAINT: SalaryMin must be positive when provided
    // Name follows convention: ck_tablename_description
    // This runs at the DATABASE level — bypasses the API completely
    entity.ToTable(t => t.HasCheckConstraint(
        "ck_job_listings_salary_min_positive",
        "salary_min IS NULL OR salary_min > 0"));

    // CHECK CONSTRAINT: SalaryMax must be greater than SalaryMin
    // Handles nulls — a null salary is allowed, min > max is not
    entity.ToTable(t => t.HasCheckConstraint(
        "ck_job_listings_salary_max_greater_than_min",
        "salary_min IS NULL OR salary_max IS NULL OR salary_max > salary_min"));

    // CHECK CONSTRAINT: ExpiresAt must be after PostedAt
    // A listing cannot expire before it was posted
    entity.ToTable(t => t.HasCheckConstraint(
        "ck_job_listings_expires_after_posted",
        "expires_at IS NULL OR expires_at > posted_at"));
});
