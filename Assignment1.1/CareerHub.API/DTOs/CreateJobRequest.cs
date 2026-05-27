using System.ComponentModel.DataAnnotations;
using CareerHub.API.Models;
namespace CareerHub.API.DTOs;

// Conference Booking equivalent: CreateBookingRequest
// This is ONLY what the client sends — no Id, no PostedAt, no IsActive
public class CreateJobRequest : IValidatableObject
{
    // [Required] = framework rejects the request before your code runs
    // if this field is missing — you write zero if-checks in the endpoint
    [Required(ErrorMessage = "Title is required")]
    [StringLength(120, MinimumLength = 5,
        ErrorMessage = "Title must be between 5 and 120 characters")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Company is required")]
    [StringLength(80, MinimumLength = 2,
        ErrorMessage = "Company must be between 2 and 80 characters")]
    public string Company { get; set; } = string.Empty;

    [Required(ErrorMessage = "Location is required")]
    public string Location { get; set; } = string.Empty;

    [Required(ErrorMessage = "Description is required")]
    [MinLength(20,
        ErrorMessage = "Description must be at least 20 characters")]
    public string Description { get; set; } = string.Empty;

    // JobType enum — only FullTime, PartTime, Contract, Internship accepted
    [Required(ErrorMessage = "Job type is required")]
    public JobType Type { get; set; }

    // ? means optional — if not provided, no validation runs
    [Range(1, double.MaxValue,
        ErrorMessage = "SalaryMin must be greater than zero")]
    public decimal? SalaryMin { get; set; }

    [Range(1, double.MaxValue,
        ErrorMessage = "SalaryMax must be greater than zero")]
    public decimal? SalaryMax { get; set; }

    // IValidatableObject: handles rules that span multiple fields
    // Data Annotations can only check ONE field at a time
    // This is the lecturer's "idempotency" concept applied to salary logic
    // The framework calls this automatically after all annotations pass
    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (SalaryMin.HasValue && SalaryMax.HasValue)
        {
            if (SalaryMax <= SalaryMin)
            {
                yield return new ValidationResult(
                    "SalaryMax must be greater than SalaryMin",
                    new[] { nameof(SalaryMax) }
                );
            }
        }
    }
}