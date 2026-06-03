namespace CareerHub.API.Models;

// Assignment 2.2 — The join entity between JobListing and Applicant
// This is NOT a hidden join table — it carries domain data:
// SubmittedAt and Status are real business concepts
// Primary key is composite: (JobListingId, ApplicantId)
// This enforces that one applicant can only apply once per listing
public class Application
{
    // Part of composite primary key
    public Guid JobListingId { get; set; }

    // Part of composite primary key
    public Guid ApplicantId { get; set; }

    // Server stamps this at submission time
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    // Tracks where the application is in the hiring workflow
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Submitted;

    // Navigation properties — EF Core uses these to follow relationships
    // Required navigation properties signal to compiler they won't be null
    public JobListing JobListing { get; set; } = null!;
    public Applicant Applicant { get; set; } = null!;
}