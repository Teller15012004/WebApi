namespace CareerHub.API.Models;

// Assignment 2.2 — Company is now a real entity, not a plain string
// One Company owns many JobListings
// Mutable class — EF Core change tracker requires settable properties
// No Data Annotations — all constraints live in CareerHubDbContext
public class Company
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;
    public string Industry { get; set; } = string.Empty;

    // Collection navigation — initialised so it is never null
    // EF Core populates this when you use Include()
    public List<JobListing> JobListings { get; set; } = new();
}