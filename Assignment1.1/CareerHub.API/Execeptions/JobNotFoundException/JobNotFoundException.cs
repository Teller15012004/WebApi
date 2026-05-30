namespace CareerHub.API.Exceptions;

// Conference Booking equivalent: BookingNotFoundException
// This exception knows about the JOB DOMAIN — not about HTTP
// It does not know what a 404 is — that is the handler's job
public class JobNotFoundException : Exception
{
    // Constructor accepts the ID and formats a clear message
    // "The job listing with ID {id} was not found."
    public JobNotFoundException(Guid id)
        : base($"The job listing with ID {id} was not found.")
    {
    }
}