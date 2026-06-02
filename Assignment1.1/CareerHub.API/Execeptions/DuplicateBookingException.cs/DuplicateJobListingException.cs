namespace CareerHub.API.Exceptions;

// Assignment 1.3 — Thrown when title + company combination already exists
// GlobalExceptionHandler maps it to 409
public class DuplicateJobListingException : Exception
{
    public DuplicateJobListingException(string title, string company)
        : base($"A job listing for '{title}' at '{company}' already exists.")
    {
    }
}