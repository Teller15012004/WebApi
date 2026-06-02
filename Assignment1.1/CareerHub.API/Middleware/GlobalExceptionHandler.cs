using CareerHub.API.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;
using System.Text.Json;

namespace CareerHub.API.Middleware;

// Assignment 1.3 — Catches ALL thrown exceptions in one place
// Maps exception types to HTTP status codes
// Builds Problem Details response — same shape for every error
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // 1. Log the error
        _logger.LogError(
            exception,
            "An exception occurred: {Message}",
            exception.Message);

        // 2. Map exception type to HTTP status code
        var statusCode = exception switch
        {
            JobNotFoundException         => StatusCodes.Status404NotFound,
            DuplicateJobListingException => StatusCodes.Status409Conflict,
            _                            => StatusCodes.Status500InternalServerError
        };

        // 3. Build and write Problem Details response
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title  = GetTitle(statusCode),
            Detail = exception.Message
        };

        httpContext.Response.StatusCode  = statusCode;
        httpContext.Response.ContentType = MediaTypeNames.Application.Json;

        await httpContext.Response.WriteAsync(
            JsonSerializer.Serialize(problemDetails),
            cancellationToken);

        return true;
    }

    private static string GetTitle(int statusCode) => statusCode switch
    {
        StatusCodes.Status404NotFound            => "Not Found",
        StatusCodes.Status409Conflict            => "Conflict",
        StatusCodes.Status500InternalServerError => "Internal Server Error",
        _                                        => "An error occurred"
    };
}