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

## Assignment 1.4 Design Decisions

### Stateless Auth — JWT vs Sessions
Session-based auth stores login state on the SERVER — a session ID in memory or a database. Every request hits that store to check if the session is still valid. JWT-based auth stores state in the TOKEN itself — the server never remembers anything. Statelessness matters for horizontal scaling because any server in a cluster can validate a JWT without talking to a shared session store. Sessions require sticky sessions or a shared cache. JWTs require nothing shared.

### 401 vs 403
401 Unauthorized means "I don't know who you are — send a token."
It is produced by UseAuthentication() when no token is present or the token is invalid. 403 Forbidden means "I know who you are but you are not allowed to do this." It is produced by UseAuthorization() when the token is valid but the role claim does not match the required role.

### JWT Storage — localStorage vs Alternatives
Storing a JWT in localStorage is risky because any JavaScript running on the page — including injected scripts from an XSS attack — can read localStorage and steal the token. The safer alternatives are HttpOnly
cookies (JavaScript cannot read them at all) or in-memory storage (lost on page refresh but never accessible to injected scripts).