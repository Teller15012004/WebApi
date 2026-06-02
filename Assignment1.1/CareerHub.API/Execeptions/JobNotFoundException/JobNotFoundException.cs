namespace CareerHub.API.Exceptions;

// Assignment 1.3 — Thrown when a job ID does not exist
// Knows nothing about HTTP — GlobalExceptionHandler maps it to 404
public class JobNotFoundException : Exception
{
    public JobNotFoundException(Guid id)
        : base($"The job listing with ID {id} was not found.")
    {
    }
}