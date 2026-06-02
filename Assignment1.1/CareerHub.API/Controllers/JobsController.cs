using CareerHub.API.DTOs;
using CareerHub.API.Exceptions;
using CareerHub.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareerHub.API.Controllers;

[ApiController]
[Route("api/jobs")]
public class JobsController : ControllerBase
{
    // In-memory store — shared via static so all requests see the same data
    // Week 2 replaces this with an actual database
    private static readonly List<JobListing> _jobs = new()
    {
        new JobListing
        {
            Id          = Guid.NewGuid(),
            Title       = "Software Engineer",
            Company     = "TechCorp",
            Location    = "Cape Town",
            Description = "Build and maintain scalable web applications for enterprise clients.",
            Type        = JobType.FullTime,
            SalaryMin   = 45000,
            SalaryMax   = 65000,
            PostedAt    = DateTime.UtcNow.AddDays(-3),
            IsActive    = true
        },
        new JobListing
        {
            Id          = Guid.NewGuid(),
            Title       = "Frontend Developer",
            Company     = "PixelStudio",
            Location    = "Johannesburg",
            Description = "Design and implement React components for our client-facing dashboard.",
            Type        = JobType.Contract,
            SalaryMin   = 30000,
            SalaryMax   = null,
            PostedAt    = DateTime.UtcNow.AddDays(-7),
            IsActive    = true
        },
        new JobListing
        {
            Id          = Guid.NewGuid(),
            Title       = "Data Analyst Intern",
            Company     = "Insightful",
            Location    = "Remote",
            Description = "Assist the analytics team with data cleaning and visualisation tasks.",
            Type        = JobType.Internship,
            SalaryMin   = null,
            SalaryMax   = null,
            PostedAt    = DateTime.UtcNow.AddDays(-1),
            IsActive    = true
        }
    };

    // GET /api/jobs — public, no token required
    // [AllowAnonymous] overrides any controller-level [Authorize]
    [HttpGet]
    [AllowAnonymous]
    public IActionResult GetAll()
    {
        var response = _jobs
            .Where(j => j.IsActive)
            .Select(JobResponse.FromModel);
        return Ok(response);
    }

    // GET /api/jobs/{id} — public, no token required
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public IActionResult GetById(Guid id)
    {
        var job = _jobs.FirstOrDefault(j => j.Id == id);
        if (job is null)
            throw new JobNotFoundException(id);

        return Ok(JobResponse.FromModel(job));
    }

    // POST /api/jobs — requires valid JWT + Employer role
    // [Authorize(Roles = "Employer")] — two checks:
    // 1. Is the token valid? If not → 401 Unauthorized
    // 2. Does the token have role = Employer? If not → 403 Forbidden
    [HttpPost]
    [Authorize(Roles = "Employer")]
    public IActionResult Create([FromBody] CreateJobRequest request)
    {
        bool duplicate = _jobs.Any(j =>
            string.Equals(j.Title,   request.Title,
                StringComparison.OrdinalIgnoreCase) &&
            string.Equals(j.Company, request.Company,
                StringComparison.OrdinalIgnoreCase));

        if (duplicate)
            throw new DuplicateJobListingException(request.Title, request.Company);

        var newJob = new JobListing
        {
            Id          = Guid.NewGuid(),
            Title       = request.Title,
            Company     = request.Company,
            Location    = request.Location,
            Description = request.Description,
            Type        = request.Type,
            SalaryMin   = request.SalaryMin,
            SalaryMax   = request.SalaryMax,
            PostedAt    = DateTime.UtcNow,
            IsActive    = true
        };

        _jobs.Add(newJob);
        return Created($"/api/jobs/{newJob.Id}", JobResponse.FromModel(newJob));
    }

    // PUT /api/jobs/{id} — requires valid JWT + Employer role
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Employer")]
    public IActionResult Update(Guid id, [FromBody] UpdateJobRequest request)
    {
        var existing = _jobs.FirstOrDefault(j => j.Id == id);
        if (existing is null)
            throw new JobNotFoundException(id);

        existing.Title       = request.Title;
        existing.Company     = request.Company;
        existing.Location    = request.Location;
        existing.Description = request.Description;
        existing.Type        = request.Type;
        existing.SalaryMin   = request.SalaryMin;
        existing.SalaryMax   = request.SalaryMax;

        return Ok(JobResponse.FromModel(existing));
    }

    // DELETE /api/jobs/{id} — requires valid JWT + Employer role
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Employer")]
    public IActionResult Delete(Guid id)
    {
        var job = _jobs.FirstOrDefault(j => j.Id == id);
        if (job is null)
            throw new JobNotFoundException(id);

        _jobs.Remove(job);
        return NoContent();
    }
}