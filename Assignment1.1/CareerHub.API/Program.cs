using System.Text;
using CareerHub.API.Data;
using CareerHub.API.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;

// Assignment 1.3 — Serilog bootstrap logger
// Catches startup errors before the app fully loads
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Assignment 1.3 — Replace default logger with Serilog
    builder.Host.UseSerilog((context, services, configuration) =>
        configuration.WriteTo.Console());

    // Assignment 1.4 — Scan Controllers/ folder and register all controllers
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            // Assignment 1.2 — Write enums as strings: "FullTime" not 0
            options.JsonSerializerOptions.Converters.Add(
                new System.Text.Json.Serialization.JsonStringEnumConverter());
        });

    builder.Services.AddOpenApi();

    // Assignment 1.2 — Consistent error shape for all errors
    builder.Services.AddProblemDetails();

    // Assignment 1.3 — Our custom global exception handler
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

    // Assignment 1.4 — CORS policy for Next.js frontend on port 3000
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            policy
                .WithOrigins("http://localhost:3000")
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
    });

    // Assignment 1.4 — Read secret key from config, never hardcoded
    var secretKey = builder.Configuration["Jwt:SecretKey"]!;
    var keyBytes  = Encoding.UTF8.GetBytes(secretKey);

    // Assignment 1.4 — JWT Bearer Authentication
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey         = new SymmetricSecurityKey(keyBytes),
                ValidateIssuer           = false,
                ValidateAudience         = false,
                ValidateLifetime         = true
            };
        });

    // Assignment 1.4 — Required for [Authorize(Roles = "Employer")] to work
    builder.Services.AddAuthorization();

    // Assignment 2.1 — Register DbContext with Npgsql provider
    // Scoped lifetime — one instance per HTTP request
    builder.Services.AddDbContext<CareerHubDbContext>(options =>
        options.UseNpgsql(
            builder.Configuration.GetConnectionString("DefaultConnection")));

    var app = builder.Build();

    // ── MIDDLEWARE PIPELINE — ORDER MATTERS ───────────────────────────────

    // Assignment 1.3 — Log every request first
    app.UseSerilogRequestLogging();

    // Assignment 1.4 — Browser preflight checks
    app.UseCors("AllowFrontend");

    // Assignment 1.3 — Safety net for all exceptions below
    app.UseExceptionHandler();

    // Assignment 1.2 — Wraps bare status codes in Problem Details
    app.UseStatusCodePages();

    // Assignment 1.4 — Reads and validates the JWT token (WHO are you?)
    app.UseAuthentication();

    // Assignment 1.4 — Checks [Authorize] attributes (WHAT can you do?)
    app.UseAuthorization();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference();
    }

    // Assignment 1.4 — Routes requests to the right controller method
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    // Assignment 1.3 — Catches fatal startup errors
    Log.Fatal(ex, "Application failed to start");
}
finally
{
    // Assignment 1.3 — Always flush logs before the process exits
    Log.CloseAndFlush();
}