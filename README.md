# CareerHub API - Assignment 1.1

## Architecture Choice
    Used Minimal APIs instead of Controllers. For 2 simple read-only endpoints, Minimal APIs have less boilerplate, faster startup, and are the recommended approach in .NET 10. Controllers would add unnecessary abstraction.

## How to Run
    1. `dotnet restore`
    2. `dotnet run`
    3. Open the Scalar UI URL shown in terminal, usually https://localhost:7227/scalar/v1

## Endpoints
    - GET /jobs - returns all job listings
    - GET /jobs/{id} - returns one job or 404 if not found

## Design Decisions

### PostedAt — Why it's in JobResponse but not CreateJobRequest
PostedAt is stamped by the server the moment a job is created.
If the client could send this value, someone could backdate a listing to make it appear more recent in search results. The server owns it — the client only ever reads it back in the response.

### Salary cross-field validation
I used IValidatableObject on CreateJobRequest and UpdateJobRequest.
Data Annotations can only validate one field at a time, so they cannot express the rule "SalaryMax must be greater than SalaryMin." The Validate() method handles this after all annotations pass, keeping the endpoint completely clean — no if-statements in the handler.

### PUT returns 200 with body
    I return 200 OK with the updated JobResponse. This means the client sees the full updated state immediately without needing a second GET request to confirm the change.

### DELETE for a missing ID returns 404
    I return 404 Not Found. On a job board, a client deleting an ID that does not exist almost certainly has a wrong or stale reference.
    Returning 204 would silently hide that mistake. A 404 forces the client to notice and handle the error correctly.

## Assignment 1.3 Design Decisions
### Controller Thinning
    Throwing JobNotFoundException instead of returning NotFound() directly keeps the controller focused on one job — the happy path. The controller should not need to know that a missing job maps to HTTP 404. That mapping lives in one place — the GlobalExceptionHandler. If we ever need to change the status code or error shape, we change it once instead of hunting through every endpoint.

### Structured Logging with Serilog
    Console.WriteLine produces plain text strings that are impossible to query or filter in production. Serilog writes structured JSON where every field
    — timestamp, level, message, exception — is a queryable property. In a real system, those logs feed into tools like Seq, Datadog, or Azure Monitor where you can filter by status code, search by job ID, or alert on error rate spikes. Plain strings cannot do any of that.