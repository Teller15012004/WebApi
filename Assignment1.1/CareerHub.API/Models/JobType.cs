namespace CareerHub.API.Models;

// Assignment 1.2 — Locks job type to four valid values
// Prevents "banana" being stored as a job type
public enum JobType
{
    FullTime,
    PartTime,
    Contract,
    Internship
}