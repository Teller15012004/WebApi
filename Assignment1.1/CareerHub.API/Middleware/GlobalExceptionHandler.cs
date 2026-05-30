using CareerHub.API.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;
using System.Text.Json;

namespace CareerHub.API.Middleware;

// Conference Booking equivalent: the GlobalExceptionHandler the lecturer showed
// IExceptionHandler is a .NET 10 interface — we implement TryHandleAsync
// This class is the ONE place in the entire app that maps exceptions to HTTP
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    // ILogger is injected — same logger Serilog will plug into in Step 4
    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // 1. Log the error before doing anything else
        _logger.LogError(
            exception,
            "An exception occurred: {Message}",
            exception.Message);

        // 2. Translate the exception type to an HTTP status code
        // Conference Booking equivalent:
        // BookingNotFoundException    → 404
        // DuplicateBookingException   → 409
        // anything else               → 500
        var statusCode = exception switch
        {
            JobNotFoundException          => StatusCodes.Status404NotFound,
            DuplicateJobListingException  => StatusCodes.Status409Conflict,
            _                             => StatusCodes.Status500InternalServerError
        };

        // 3. Build the Problem Details response shape (RFC 7807)
        // This is the ONE place we construct error responses
        // No controller ever builds an error response again
        var problemDetails = new ProblemDetails
        {
            Status  = statusCode,
            Title   = GetTitle(statusCode),
            Detail  = exception.Message
        };

        // Write the Problem Details as JSON to the response
        httpContext.Response.StatusCode  = statusCode;
        httpContext.Response.ContentType = MediaTypeNames.Application.Json;

        await httpContext.Response.WriteAsync(
            JsonSerializer.Serialize(problemDetails),
            cancellationToken);

        // Return true = we handled it, stop looking for other handlers
        return true;
    }

    // Helper — maps a status code to a human-readable title
    // Conference Booking equivalent: GetTitle in the lecturer's code
    private static string GetTitle(int statusCode) => statusCode switch
    {
        StatusCodes.Status404NotFound           => "Not Found",
        StatusCodes.Status409Conflict           => "Conflict",
        StatusCodes.Status500InternalServerError => "Internal Server Error",
        _                                        => "An error occurred"
    };
}