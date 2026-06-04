using CareerHub.API.Data;
using CareerHub.API.DTOs;
using CareerHub.API.Exceptions;
using CareerHub.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CareerHub.API.Controllers;

[ApiController]
[Route("api/jobs")]
public class JobsController : ControllerBase
{
    private readonly CareerHubDbContext _context;

    public JobsController(CareerHubDbContext context)
    {
        _context = context;
    }

    // GET /api/jobs — public
    // Assignment 2.2 fixes applied:
    // 1. AsNoTracking() — read-only query, no change tracking overhead
    // 2. Include(Company) — eager load in ONE query, fixes N+1 problem
    // 3. Select() projection — only load columns the DTO actually needs
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll()
    {
        // AsNoTracking — we are not saving changes after this
        // Skips change tracker snapshot — faster for read-only queries
        // Without AsNoTracking EF Core snapshots every loaded entity
        // which costs memory and CPU you do not need for a GET

        // Include(j => j.Company) — eager loading
        // Tells EF Core to JOIN companies table in the SAME query
        // Without this: N+1 queries (one per listing to load company)
        // With this: exactly ONE query with a JOIN clause

        // Select() projection — only transfer columns the DTO needs
        // Without projection: SELECT * loads website, industry etc.
        // that never appear in the response — wasted database bandwidth
        var jobs = await _context.JobListings
            .AsNoTracking()
            .Where(j => j.IsActive)
            .Include(j => j.Company)
            .Select(j => new JobListingResponse
            {
                Id               = j.Id,
                Title            = j.Title,
                CompanyName      = j.Company.Name,   // from JOIN
                Location         = j.Location,
                Description      = j.Description,
                Type             = j.Type.ToString(),
                SalaryMin        = j.SalaryMin,
                SalaryMax        = j.SalaryMax,
                SalaryDisplay    = j.SalaryMin != null && j.SalaryMax != null
                    ? $"R{j.SalaryMin:N0} – R{j.SalaryMax:N0}/month"
                    : j.SalaryMin != null
                        ? $"From R{j.SalaryMin:N0}/month"
                        : "Salary not specified",
                PostedAt         = j.PostedAt,
                IsActive         = j.IsActive,
                // Count computed by database — not .Count() on a loaded list
                ApplicationCount = j.Applications.Count()
            })
            .ToListAsync();

        return Ok(jobs);
    }

    // GET /api/jobs/{id} — public
    // Loads listing + company + applications + applicant names
    // Only fields the response body exposes — no extra columns
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id)
    {
        // AsNoTracking — read only, no save after this
        // Include chains load related data in one query
        var job = await _context.JobListings
            .AsNoTracking()
            .Include(j => j.Company)
            .Include(j => j.Applications)
                .ThenInclude(a => a.Applicant)
            .FirstOrDefaultAsync(j => j.Id == id);

        if (job is null)
            throw new JobNotFoundException(id);

        // Map to response — only expose what the DTO declares
        var response = new
        {
            job.Id,
            job.Title,
            CompanyName  = job.Company.Name,
            job.Location,
            job.Description,
            Type         = job.Type.ToString(),
            job.SalaryMin,
            job.SalaryMax,
            job.PostedAt,
            job.IsActive,
            Applications = job.Applications.Select(a => new
            {
                a.ApplicantId,
                ApplicantName = a.Applicant.FullName,
                a.SubmittedAt,
                Status = a.Status.ToString()
            })
        };

        return Ok(response);
    }

    // POST /api/jobs — Employer role required
    [HttpPost]
    [Authorize(Roles = "Employer")]
    public async Task<IActionResult> Create([FromBody] CreateJobRequest request)
    {
        // Duplicate check — same title + company
        bool duplicate = await _context.JobListings.AnyAsync(j =>
            j.Title.ToLower()     == request.Title.ToLower() &&
            j.CompanyId           == request.CompanyId);

        if (duplicate)
            throw new DuplicateJobListingException(request.Title, request.CompanyId.ToString());

        var newJob = new JobListing
        {
            Id          = Guid.NewGuid(),
            Title       = request.Title,
            CompanyId   = request.CompanyId,
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

        return Created($"/api/jobs/{newJob.Id}", newJob);
    }

    // PUT /api/jobs/{id} — Employer role required
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Employer")]
    public async Task<IActionResult> Update(Guid id,
        [FromBody] UpdateJobRequest request)
    {
        // No AsNoTracking here — we ARE saving changes after this
        // Change tracker must watch this entity to generate UPDATE SQL
        var existing = await _context.JobListings.FindAsync(id);

        if (existing is null)
            throw new JobNotFoundException(id);

            existing.Title       = request.Title;
            existing.CompanyId   = request.CompanyId;
            existing.Location    = request.Location;
            existing.Description = request.Description;
            existing.Type        = request.Type;
            existing.SalaryMin   = request.SalaryMin;
            existing.SalaryMax   = request.SalaryMax;

        await _context.SaveChangesAsync();

        return Ok(existing);
    }

    // DELETE /api/jobs/{id} — Employer role required
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Employer")]
    public async Task<IActionResult> Delete(Guid id)
    {
        // No AsNoTracking — we ARE calling SaveChangesAsync after Remove()
        var job = await _context.JobListings.FindAsync(id);

        if (job is null)
            throw new JobNotFoundException(id);

        _context.JobListings.Remove(job);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}