namespace CareerHub.API.Models;

// Assignment 2.2 — Applicant registers once and can apply to many listings
// One Applicant submits many Applications
// No Data Annotations — all constraints live in CareerHubDbContext
public class Applicant
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    // Collection navigation — all applications this person has submitted
    public List<Application> Applications { get; set; } = new();
}