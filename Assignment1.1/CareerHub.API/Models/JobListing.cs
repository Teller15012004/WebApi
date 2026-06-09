namespace CareerHub.API.Models;

// Assignment 1.1 — Original model
// Assignment 1.2 — Added PostedAt and IsActive
// Assignment 2.1 — Converted to mutable class for EF Core
// Assignment 2.2 — Replaced Company string with CompanyId foreign key
//                  Added Applications navigation collection
public class JobListing
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;

    // Assignment 2.2 — Foreign key pointing to companies table
    // Replaces the plain Company string from previous assignments
    public Guid CompanyId { get; set; }

    // Navigation property — EF Core populates this with Include()
    // null! tells compiler this will be set by EF Core — not null at runtime
    public Company Company { get; set; } = null!;

    public string Location { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public JobType Type { get; set; }
    public decimal? SalaryMin { get; set; }
    public decimal? SalaryMax { get; set; }
    public DateTime PostedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    // Assignment 2.2 — All applications received for this listing
    public List<Application> Applications { get; set; } = new();

// Assignment 2.4 — Listings can expire
// NULL means the listing never expires
public DateTime? ExpiresAt { get; set; }

// Assignment 2.4 — Computed column for full-text search
// PostgreSQL generates this from Title + Description automatically
// We never set this in C# — the database maintains it
public string? SearchVector { get; set; }
}