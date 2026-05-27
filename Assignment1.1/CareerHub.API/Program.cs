using CareerHub.API.Models;
using CareerHub.API.Services;
using Microsoft.AspNetCore.OpenApi;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddSingleton<JobListingService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi(); // .NET 10 OpenAPI

var app = builder.Build();

// Enable Scalar UI
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // Auto available in .NET 10 template
}

app.UseHttpsRedirection();

var jobs = app.MapGroup("/jobs");

// GET /jobs
jobs.MapGet("/", async (JobListingService service) =>
{
    var result = await service.GetAllAsync();
    return Results.Ok(result);
});

// GET /jobs/{id}
jobs.MapGet("/{id:int}", async (int id, JobListingService service) =>
{
    var job = await service.GetByIdAsync(id);
    return job is not null ? Results.Ok(job) : Results.NotFound();
});

app.Run();