using CareerHub.API.Models;

namespace CareerHub.API.DTOs;

// Assignment 1.2 — What clients receive
// SalaryDisplay is computed here — does not exist in JobListing model
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
    public string SalaryDisplay { get; set; } = string.Empty;
    public DateTime PostedAt { get; set; }
    public bool IsActive { get; set; }

    // Maps JobListing model to JobResponse
    // Computes SalaryDisplay in one place
    public static JobResponse FromModel(JobListing job)
    {
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