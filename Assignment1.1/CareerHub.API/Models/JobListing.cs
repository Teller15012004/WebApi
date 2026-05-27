namespace CareerHub.API.Models;

public record JobListing(
    int Id,
    string Title,
    string Description,
    string Company,
    string Location,
    string Type
);