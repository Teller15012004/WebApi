using CareerHub.API.Models;
using CareerHub.API.DTOs;
using CareerHub.API.Services;
using Microsoft.AspNetCore.OpenApi;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddSingleton<JobListingService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi(); // .NET 10 OpenAPI

var app = builder.Build();

// JSON serialiser: write enums as strings not numbers
// "type": "FullTime" instead of "type": 0
// Conference Booking equivalent: "status": "Confirmed" instead of "status": 1
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(
        new System.Text.Json.Serialization.JsonStringEnumConverter());
});

// AddProblemDetails — registers the service that formats ALL errors
// as RFC 7807 Problem Details JSON — one consistent error shape everywhere
builder.Services.AddProblemDetails();



// UseExceptionHandler — catches unhandled exceptions (code crashes, null refs)
// and returns a 500 Problem Details instead of an ugly stack trace to the client
app.UseExceptionHandler();

// UseStatusCodePages — catches bare status codes with no body
// e.g. a raw 404 gets wrapped into a proper Problem Details response
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// ── In-memory store (replaces a database for now) ────────────────────────
var jobs = new List<JobListing>
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
        Description = "Design and implement React components for our client-facing dashboard product.",
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
        Description = "Assist the analytics team with data cleaning, reporting and visualisation tasks.",
        Type        = JobType.Internship,
        SalaryMin   = null,
        SalaryMax   = null,
        PostedAt    = DateTime.UtcNow.AddDays(-1),
        IsActive    = true
    }
};

// ── GET /jobs ─────────────────────────────────────────────────────────────
// Conference Booking equivalent: GET /bookings
app.MapGet("/jobs", () =>
{
    var response = jobs
        .Where(j => j.IsActive)
        .Select(JobResponse.FromModel);  // model → DTO before sending
    return Results.Ok(response);
});

// ── GET /jobs/{id} ────────────────────────────────────────────────────────
app.MapGet("/jobs/{id:guid}", (Guid id) =>
{
    var job = jobs.FirstOrDefault(j => j.Id == id);

    if (job is null)
        return Results.Problem(
            title: "Job not found",
            detail: $"No job exists with ID {id}.",
            statusCode: 404);

    return Results.Ok(JobResponse.FromModel(job));
});

// ── POST /jobs ────────────────────────────────────────────────────────────
// Conference Booking equivalent: POST /bookings
// Framework validates CreateJobRequest BEFORE this method runs
// If validation fails → automatic 400 Problem Details, this code never executes
app.MapPost("/jobs", (CreateJobRequest request) =>
{
    // Idempotency / duplicate guard
    // Conference Booking equivalent: checking if room is already booked
    // Case-insensitive: "Software Engineer at TechCorp" == "software engineer at techcorp"
    bool duplicate = jobs.Any(j =>
        string.Equals(j.Title,   request.Title,
            StringComparison.OrdinalIgnoreCase) &&
        string.Equals(j.Company, request.Company,
            StringComparison.OrdinalIgnoreCase));

    if (duplicate)
        return Results.Problem(
            title: "Duplicate job listing",
            detail: $"A listing for '{request.Title}' at '{request.Company}' already exists.",
            statusCode: 409);

    // Build the model — server sets PostedAt and IsActive
    // Client never touches these fields
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
        PostedAt    = DateTime.UtcNow,  // server stamps this
        IsActive    = true              // server defaults this
    };

    jobs.Add(newJob);

    // 201 Created + Location header pointing to GET /jobs/{id}
    // Client knows exactly where to fetch the new resource
    return Results.Created(
        $"/jobs/{newJob.Id}",
        JobResponse.FromModel(newJob));
});

// ── PUT /jobs/{id} ────────────────────────────────────────────────────────
// Fully replaces editable fields — preserves server-owned fields
// Conference Booking equivalent: PUT /bookings/{id} to reschedule a room
app.MapPut("/jobs/{id:guid}", (Guid id, UpdateJobRequest request) =>
{
    var existing = jobs.FirstOrDefault(j => j.Id == id);

    if (existing is null)
        return Results.Problem(
            title: "Job not found",
            detail: $"No job exists with ID {id}.",
            statusCode: 404);

    // Update only the editable fields
    // PostedAt and IsActive are NOT touched — a PUT must not reset server-owned fields
    existing.Title       = request.Title;
    existing.Company     = request.Company;
    existing.Location    = request.Location;
    existing.Description = request.Description;
    existing.Type        = request.Type;
    existing.SalaryMin   = request.SalaryMin;
    existing.SalaryMax   = request.SalaryMax;

    // 200 with body — client sees the full updated job immediately
    // No second GET needed to confirm what changed
    return Results.Ok(JobResponse.FromModel(existing));
});

// ── DELETE /jobs/{id} ────────────────────────────────────────────────────
// Conference Booking equivalent: DELETE /bookings/{id} to cancel a booking
app.MapDelete("/jobs/{id:guid}", (Guid id) =>
{
    var job = jobs.FirstOrDefault(j => j.Id == id);

    // Returning 404 — on a job board, deleting an unknown ID means
    // the client has a wrong or stale reference. Silently returning 204
    // would hide that mistake. 404 forces the client to notice and fix it.
    if (job is null)
        return Results.Problem(
            title: "Job not found",
            detail: $"No job exists with ID {id}.",
            statusCode: 404);

    jobs.Remove(job);
    return Results.NoContent(); // 204 — deleted, nothing to return
});

app.Run();