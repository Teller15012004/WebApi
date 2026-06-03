namespace CareerHub.API.Models;

// Assignment 2.2 — Valid status values for a job application
// Locks status to these four values — no free-text strings
public enum ApplicationStatus
{
    Submitted,   // just applied
    Reviewing,   // employer is reviewing
    Accepted,    // got the job
    Rejected     // not selected
}