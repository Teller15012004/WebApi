using System.ComponentModel.DataAnnotations;

namespace CareerHub.API.DTOs;

// Assignment 2.2 — What the client sends to create a company
public class CreateCompanyRequest
{
    [Required(ErrorMessage = "Company name is required")]
    [StringLength(100, MinimumLength = 2,
        ErrorMessage = "Name must be between 2 and 100 characters")]
    public string Name { get; set; } = string.Empty;

    public string? Website { get; set; }
    public string? Industry { get; set; }
}