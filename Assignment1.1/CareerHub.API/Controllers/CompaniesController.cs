using CareerHub.API.Data;
using CareerHub.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CareerHub.API.DTOs;

namespace CareerHub.API.Controllers;

// Assignment 2.2 — Manage company records
// Companies must exist before job listings can reference them
[ApiController]
[Route("api/companies")]
public class CompaniesController : ControllerBase
{
    private readonly CareerHubDbContext _context;

    public CompaniesController(CareerHubDbContext context)
    {
        _context = context;
    }

    // GET /api/companies — public
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll()
    {
        // AsNoTracking — read only query
        var companies = await _context.Companies
            .AsNoTracking()
            .Select(c => new { c.Id, c.Name, c.Industry, c.Website })
            .ToListAsync();

        return Ok(companies);
    }

    // POST /api/companies — Employer role required
    [HttpPost]
    [Authorize(Roles = "Employer")]
    public async Task<IActionResult> Create([FromBody] CreateCompanyRequest request)
    {
        // Check for duplicate company name
        bool exists = await _context.Companies
            .AnyAsync(c => c.Name.ToLower() == request.Name.ToLower());

        if (exists)
            return Conflict(new { message = $"Company '{request.Name}' already exists." });

        var company = new Company
        {
            Id       = Guid.NewGuid(),
            Name     = request.Name,
            Website  = request.Website ?? string.Empty,
            Industry = request.Industry ?? string.Empty
        };

        _context.Companies.Add(company);
        await _context.SaveChangesAsync();

        return Created($"/api/companies/{company.Id}", company);
    }
}