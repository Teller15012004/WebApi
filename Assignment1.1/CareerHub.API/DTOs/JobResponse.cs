using CareerHub.API.Models;

namespace CareerHub.API.DTOs;

// Assignment 2.2 — Updated response DTO
// Now includes CompanyName (from join) and ApplicationCount (computed by DB)
// SalaryDisplay still computed during mapping — not stored anywhere
public class JobListingResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;

    // Assignment 2.2 — Company name from joined companies table
    public string CompanyName { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal? SalaryMin { get; set; }
    public decimal? SalaryMax { get; set; }
    public string SalaryDisplay { get; set; } = string.Empty;
    public DateTime PostedAt { get; set; }
    public bool IsActive { get; set; }

    // Assignment 2.2 — Count computed by database, not loaded into memory
    public int ApplicationCount { get; set; }
}