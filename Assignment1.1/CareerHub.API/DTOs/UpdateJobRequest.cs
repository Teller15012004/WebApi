using System.ComponentModel.DataAnnotations;
using CareerHub.API.Models;
namespace CareerHub.API.DTOs;
// Same validation rules as CreateJobRequest
// ID comes from the route — never the body
// PUT /jobs/{id} not PUT /jobs with id in body
public class UpdateJobRequest : IValidatableObject
{
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

    [Required(ErrorMessage = "Job type is required")]
    public JobType Type { get; set; }

    [Range(1, double.MaxValue,
        ErrorMessage = "SalaryMin must be greater than zero")]
    public decimal? SalaryMin { get; set; }

    [Range(1, double.MaxValue,
        ErrorMessage = "SalaryMax must be greater than zero")]
    public decimal? SalaryMax { get; set; }

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