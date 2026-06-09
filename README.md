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

## Assignment 2.1 Design Decisions

### The Change Tracker
EF Core's change tracker watches every entity you load from the database. When you call FindAsync, it takes a snapshot of the entity's state. When you later mutate properties and call SaveChangesAsync, EF Core compares the current state against that snapshot and generates only the SQL needed to apply the differences. SaveChangesAsync is called once at the end because it wraps all changes in a single database transaction — either everything saves or nothing does.
Calling it once per property change would mean one transaction per property, which is slower and risks partial saves if one fails.

### Migrations as Version Control
The migration file must be committed alongside the code that caused it because the migration IS the database schema at that point in time.
If a teammate pulls code that references a migration they have not applied, their database schema is out of sync with the application code.
EF Core will throw an error on startup or at runtime because it expects columns that do not exist yet. Running dotnet ef database update after pulling is the fix — but only if the migration file isin source control.

### Connection String Security
The connection string belongs in appsettings.Development.json because that file is excluded from source control via .gitignore. appsettings.json is committed to GitHub — putting credentials there is a real security incident. For production, the safer alternative is environment variables or a secrets manager like Azure Key Vault or AWS Secrets Manager, where credentials are injected at runtime and never stored in files.

## Assignment 2.2 Design Decisions

### N+1 Problem
Before the fix, GET /api/jobs produced 6 SQL queries for 5 listings.
One query loaded all listings, then one separate query per listing
loaded its company. In production with 1000 listings this becomes
1001 queries per request — each one a separate database round-trip.
The fix was adding .Include(j => j.Company) which tells EF Core to
JOIN the companies table in the same query. After the fix, GET /api/jobs
always produces exactly one SQL statement regardless of how many
listings exist.

### Read vs Write Queries
A GET endpoint with AsNoTracking() skips the change tracker snapshot.
EF Core does not need to remember the original state of entities it
will never modify. This saves memory and CPU on every read.
A write endpoint (PUT, DELETE) must NOT use AsNoTracking() because
the change tracker is what detects which properties changed and
generates the targeted UPDATE SQL. If you accidentally use AsNoTracking
on a write operation, EF Core loads the entity but does not track it.
When you mutate properties and call SaveChangesAsync(), the change
tracker sees no changes — nothing is written to the database.
The save appears to succeed (no exception) but the data is not updated. This is a silent data loss bug.

## Assignment 2.4 Design Decisions

### 1. Constraint Placement
The service validates SalaryMin <= SalaryMax when a listing is
created via the API. But the service can be bypassed in three
specific scenarios:
- A developer runs a direct INSERT in psql during an incident
- A database migration script bulk-inserts rows without going
  through the API
- A bug in a future service method forgets to call the validator

If the database has no check constraint, all three scenarios
silently corrupt data. A listing with SalaryMin=80000 and
SalaryMax=20000 would be stored and served to job seekers.
The database constraint is the last line of defence — it enforces
the rule even when the application layer is bypassed.

### 2. Index Column Ordering
Query 1: WHERE CompanyId = X AND Status = 'Active'
→ CompanyId goes first. It is the high-selectivity column —
  it filters down to one company's listings immediately.
  Status is then applied to that small set.

Query 2: WHERE ExpiresAt < @threshold AND Status = 'Active'
→ Status goes first. PostgreSQL can only use a composite index
  if it filters on the leftmost column. A query that filters
  only on ExpiresAt (the second column) cannot use a composite
  index that starts with Status — it would fall back to a
  sequential scan. Putting Status first lets both queries use
  their respective indexes efficiently.

### 3. Hot Paths
Method 1: GetActiveListingsAsync()
Called on every single page load of the job board. With 1000
daily active users and an average of 10 page loads per session,
this runs ~10,000 times per day. Pre-compiling eliminates
repeated query plan compilation overhead on every call.

Method 2: HasAppliedAsync(jobListingId, applicantId)
Called every time any user opens a job detail page to check
if the button should say "Apply" or "Applied". Same 10,000
daily call volume. The query is simple and identical every
time — perfect candidate for a compiled query.

### 4. FromSql Scope
The application statistics query requires RANK() OVER
(ORDER BY total_applications DESC) — a window function.
EF Core's LINQ translator has no equivalent for window
functions. It also requires COUNT(*) FILTER (WHERE status = X)
— conditional aggregation — which EF Core cannot translate
to SQL from LINQ. Raw SQL is the only option here.