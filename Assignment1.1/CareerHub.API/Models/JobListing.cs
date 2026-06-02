namespace CareerHub.API.Models;

// Assignment 1.1 — Original model
// Assignment 1.2 — Added PostedAt and IsActive (server-owned fields)
// Assignment 2.1 — Converted to mutable class for EF Core change tracker
//                  Removed all Data Annotations — constraints live in DbContext
public class JobListing
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public JobType Type { get; set; }
    public decimal? SalaryMin { get; set; }
    public decimal? SalaryMax { get; set; }

    // Assignment 1.2 — Server stamps this at creation
    public DateTime PostedAt { get; set; } = DateTime.UtcNow;

    // Assignment 1.2 — Server defaults this to true
    public bool IsActive { get; set; } = true;
}