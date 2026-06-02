using CareerHub.API.Data;
using CareerHub.API.DTOs;
using CareerHub.API.Exceptions;
using CareerHub.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CareerHub.API.Controllers;

// Assignment 1.4 — Moved from minimal API endpoints to a controller
// Assignment 2.1 — Replaced in-memory list with EF Core database operations
[ApiController]
[Route("api/jobs")]
public class JobsController : ControllerBase
{
    private readonly CareerHubDbContext _context;

    // Assignment 2.1 — DbContext injected by DI container
    public JobsController(CareerHubDbContext context)
    {
        _context = context;
    }

    // GET /api/jobs — public
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll()
    {
        var jobs = await _context.JobListings
            .Where(j => j.IsActive)
            .ToListAsync();

        return Ok(jobs.Select(JobResponse.FromModel));
    }

    // GET /api/jobs/{id} — public
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id)
    {
        var job = await _context.JobListings.FindAsync(id);

        if (job is null)
            throw new JobNotFoundException(id);

        return Ok(JobResponse.FromModel(job));
    }

    // POST /api/jobs — Employer role required
    [HttpPost]
    [Authorize(Roles = "Employer")]
    public async Task<IActionResult> Create([FromBody] CreateJobRequest request)
    {
        bool duplicate = await _context.JobListings.AnyAsync(j =>
            j.Title.ToLower()   == request.Title.ToLower() &&
            j.Company.ToLower() == request.Company.ToLower());

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

        _context.JobListings.Add(newJob);
        await _context.SaveChangesAsync();

        return Created($"/api/jobs/{newJob.Id}", JobResponse.FromModel(newJob));
    }

    // PUT /api/jobs/{id} — Employer role required
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Employer")]
    public async Task<IActionResult> Update(Guid id,
        [FromBody] UpdateJobRequest request)
    {
        var existing = await _context.JobListings.FindAsync(id);

        if (existing is null)
            throw new JobNotFoundException(id);

        existing.Title       = request.Title;
        existing.Company     = request.Company;
        existing.Location    = request.Location;
        existing.Description = request.Description;
        existing.Type        = request.Type;
        existing.SalaryMin   = request.SalaryMin;
        existing.SalaryMax   = request.SalaryMax;

        await _context.SaveChangesAsync();

        return Ok(JobResponse.FromModel(existing));
    }

    // DELETE /api/jobs/{id} — Employer role required
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Employer")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var job = await _context.JobListings.FindAsync(id);

        if (job is null)
            throw new JobNotFoundException(id);

        _context.JobListings.Remove(job);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}