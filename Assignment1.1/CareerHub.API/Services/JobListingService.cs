using CareerHub.API.Models;

namespace CareerHub.API.Services;

public class JobListingService
{
    private static readonly List<JobListing> _jobs = new()
    {
        new(1, "Backend Developer", "Build APIs with .NET 10", "TechCorp", "Remote", "Full-time"),
        new(2, "Frontend Engineer", "Work on React/Next.js UI", "Webify", "Cape Town", "Contract"),
        new(3, "DevOps Intern", "CI/CD and Docker basics", "CloudUp", "Johannesburg", "Internship")
    };

    public Task<List<JobListing>> GetAllAsync() => Task.FromResult(_jobs);

    public Task<JobListing?> GetByIdAsync(int id) => Task.FromResult(_jobs.FirstOrDefault(j => j.Id == id));
}