using CareerHub.API.DTOs;
using CareerHub.API.Exceptions;
using CareerHub.API.Middleware;
using CareerHub.API.Models;
using Serilog;
using Scalar.AspNetCore;


// Configure Serilog at the VERY TOP — before anything else
// This ensures startup errors are caught and logged
// Conference Booking equivalent: the LoggerConfiguration the lecturer showed
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Replace the default ASP.NET logger with Serilog
    builder.Host.UseSerilog((context, services, configuration) =>
        configuration.WriteTo.Console());

    builder.Services.AddOpenApi();

    // JSON serialiser: write enums as strings not numbers
    builder.Services.ConfigureHttpJsonOptions(options =>
    {
        options.SerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

    // Register Problem Details service
    builder.Services.AddProblemDetails();

    // Register our GlobalExceptionHandler
    // This is the line the assignment requires — wires up our custom handler
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

    var app = builder.Build();

    // UseExceptionHandler — activates the IExceptionHandler pipeline
    // Our GlobalExceptionHandler gets called from here
    app.UseExceptionHandler();

    // UseSerilogRequestLogging — logs every HTTP request automatically
    // Shows method, path, status code, and response time in terminal
    // Must come AFTER UseExceptionHandler so errors are caught first
    app.UseSerilogRequestLogging();

    // UseStatusCodePages — wraps bare status codes in Problem Details
    app.UseStatusCodePages();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference();
    }

    // ── In-memory store ───────────────────────────────────────────────────
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

    // ── GET /jobs ─────────────────────────────────────────────────────────
    app.MapGet("/jobs", () =>
    {
        var response = jobs
            .Where(j => j.IsActive)
            .Select(JobResponse.FromModel);
        return Results.Ok(response);
    });

    // ── GET /jobs/{id} ────────────────────────────────────────────────────
    // BEFORE (1.2): if (job is null) return Results.Problem(statusCode: 404)
    // AFTER  (1.3): just throw — GlobalExceptionHandler does the rest
    app.MapGet("/jobs/{id:guid}", (Guid id) =>
    {
        var job = jobs.FirstOrDefault(j => j.Id == id);
        if (job is null)
            throw new JobNotFoundException(id);

        return Results.Ok(JobResponse.FromModel(job));
    });

    // ── POST /jobs ────────────────────────────────────────────────────────
    app.MapPost("/jobs", (CreateJobRequest request) =>
    {
        bool duplicate = jobs.Any(j =>
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

        jobs.Add(newJob);
        return Results.Created($"/jobs/{newJob.Id}", JobResponse.FromModel(newJob));
    });

    // ── PUT /jobs/{id} ────────────────────────────────────────────────────
    app.MapPut("/jobs/{id:guid}", (Guid id, UpdateJobRequest request) =>
    {
        var existing = jobs.FirstOrDefault(j => j.Id == id);
        if (existing is null)
            throw new JobNotFoundException(id);

        existing.Title       = request.Title;
        existing.Company     = request.Company;
        existing.Location    = request.Location;
        existing.Description = request.Description;
        existing.Type        = request.Type;
        existing.SalaryMin   = request.SalaryMin;
        existing.SalaryMax   = request.SalaryMax;

        return Results.Ok(JobResponse.FromModel(existing));
    });

    // ── DELETE /jobs/{id} ─────────────────────────────────────────────────
    app.MapDelete("/jobs/{id:guid}", (Guid id) =>
    {
        var job = jobs.FirstOrDefault(j => j.Id == id);
        if (job is null)
            throw new JobNotFoundException(id);

        jobs.Remove(job);
        return Results.NoContent();
    });

    app.Run();
}
catch (Exception ex)
{
    // Catches fatal startup errors — Serilog logs them before the app dies
    Log.Fatal(ex, "Application failed to start");
}
finally
{
    // Always flush logs before the process exits
    Log.CloseAndFlush();
}