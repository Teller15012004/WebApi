namespace CareerHub.API.Models;
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

    // Server owns these — client never sends them
    // PostedAt: stamped at creation — like BookedAt on a room booking
    // IsActive: defaults true — like IsConfirmed on a booking
    public DateTime PostedAt { get; set; }
    public bool IsActive { get; set; }
}