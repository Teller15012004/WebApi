namespace CareerHub.API.Exceptions;

// Conference Booking equivalent: DuplicateBookingException
// Thrown when someone tries to post a job that already exists
// Accepts title and company — formats a meaningful message
public class DuplicateJobListingException : Exception
{
    public DuplicateJobListingException(string title, string company)
        : base($"A job listing for '{title}' at '{company}' already exists.")
    {
    }
}