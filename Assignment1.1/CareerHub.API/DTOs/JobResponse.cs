using CareerHub.Models;

namespace CareerHub.API.DTOs;
// Conference Booking equivalent: BookingResponse
// This is ONLY what the client receives
// SalaryDisplay does not exist in JobListing — we compute it here
public class JobResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal? SalaryMin { get; set; }
    public decimal? SalaryMax { get; set; }

    // Computed — not stored anywhere in the model
    // Client gets "R25,000 – R40,000/month" instead of two raw numbers
    public string SalaryDisplay { get; set; } = string.Empty;

    // Server set this — client can now display "Posted 3 days ago"
    public DateTime PostedAt { get; set; }
    public bool IsActive { get; set; }

    // Static factory — maps JobListing → JobResponse in one place
    // Every endpoint calls this instead of duplicating mapping logic
    public static JobResponse FromModel(JobListing job)
    {
        // Pattern matching to build the salary string
        // Conference Booking equivalent: computing "Room A, Floor 3"
        // from separate RoomNumber and Floor fields
        string salaryDisplay = job switch
        {
            { SalaryMin: not null, SalaryMax: not null }
                => $"R{job.SalaryMin:N0} – R{job.SalaryMax:N0}/month",
            { SalaryMin: not null }
                => $"From R{job.SalaryMin:N0}/month",
            _ => "Salary not specified"
        };

        return new JobResponse
        {
            Id            = job.Id,
            Title         = job.Title,
            Company       = job.Company,
            Location      = job.Location,
            Description   = job.Description,
            Type          = job.Type.ToString(),
            SalaryMin     = job.SalaryMin,
            SalaryMax     = job.SalaryMax,
            SalaryDisplay = salaryDisplay,
            PostedAt      = job.PostedAt,
            IsActive      = job.IsActive
        };
    }
}