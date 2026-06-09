using CareerHub.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CareerHub.API.Data;

// Assignment 2.1 — Original DbContext
// Assignment 2.2 — Added Company, Applicant, Application entities
// Assignment 2.4 — Added check constraints and indexes
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
                  .IsUnique()
                  .HasDatabaseName("ix_companies_name");
        });

        // ── JobListing ────────────────────────────────────────────────────
        // ONE single block for JobListing — all config lives here together
        modelBuilder.Entity<JobListing>(entity =>
        {
            entity.ToTable("job_listings");

            entity.HasKey(e => e.Id);

            // We supply the Guid — database does not generate it
            entity.Property(e => e.Id)
                  .ValueGeneratedNever();

            entity.Property(e => e.Title)
                  .IsRequired()
                  .HasMaxLength(120);

            entity.Property(e => e.Location)
                  .IsRequired();

            entity.Property(e => e.Description)
                  .IsRequired();

            // Assignment 2.2 — Company → JobListing relationship
            // Restrict = block deletion of a company that has listings
            entity.HasOne(j => j.Company)
                  .WithMany(c => c.JobListings)
                  .HasForeignKey(j => j.CompanyId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Unique index on Title + CompanyId
            entity.HasIndex(e => new { e.Title, e.CompanyId })
                  .IsUnique()
                  .HasDatabaseName("ix_job_listings_title_company_id");

            // Assignment 2.4 — Indexes for query performance
            // Active listings query — called on every job board page load
            entity.HasIndex(e => new { e.IsActive, e.ExpiresAt })
                  .HasDatabaseName("ix_job_listings_is_active_expires_at");

            // Company-scoped listings — employer views their own posts
            entity.HasIndex(e => new { e.CompanyId, e.IsActive })
                  .HasDatabaseName("ix_job_listings_company_id_is_active");

            // Assignment 2.4 — Check constraints
            // Enforced at DATABASE level — cannot be bypassed by the API

            // SalaryMin must be positive when provided
            entity.ToTable(t => t.HasCheckConstraint(
                "ck_job_listings_salary_min_positive",
                "salary_min IS NULL OR salary_min > 0"));

            // SalaryMax must be greater than SalaryMin when both provided
            entity.ToTable(t => t.HasCheckConstraint(
                "ck_job_listings_salary_max_greater_than_min",
                "salary_min IS NULL OR salary_max IS NULL OR salary_max > salary_min"));

            // ExpiresAt must be after PostedAt
            entity.ToTable(t => t.HasCheckConstraint(
                "ck_job_listings_expires_after_posted",
                "expires_at IS NULL OR expires_at > posted_at"));
        });

        // ── Applicant ─────────────────────────────────────────────────────
        modelBuilder.Entity<Applicant>(entity =>
        {
            entity.ToTable("applicants");

            entity.HasKey(e => e.Id);

            // We supply the Guid — database does not generate it
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
                  .IsUnique()
                  .HasDatabaseName("ix_applicants_email");
        });

        // ── Application (join entity) ─────────────────────────────────────
        // ONE single block for Application — all config lives here together
        modelBuilder.Entity<Application>(entity =>
        {
            entity.ToTable("applications");

            // Composite primary key — one application per applicant per listing
            entity.HasKey(e => new { e.JobListingId, e.ApplicantId });

            // Application → JobListing relationship
            // Cascade = if a listing is deleted, remove its applications
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

            // Assignment 2.4 — Indexes for application queries
            // HasAppliedAsync check — called on every job detail page view
            entity.HasIndex(e => new { e.JobListingId, e.ApplicantId })
                  .HasDatabaseName("ix_applications_job_listing_id_applicant_id");

            // Employer dashboard — all applications for a listing
            entity.HasIndex(e => e.JobListingId)
                  .HasDatabaseName("ix_applications_job_listing_id");

            // Assignment 2.4 — SubmittedAt cannot be in the future
            entity.ToTable(t => t.HasCheckConstraint(
                "ck_applications_submitted_at_not_future",
                "submitted_at <= NOW()"));
        });
    }
}