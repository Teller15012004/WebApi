using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CareerHub.API.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace CareerHub.API.Controllers;

// Assignment 1.4 — Handles login and identity endpoints
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public AuthController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    // POST /api/auth/login
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        bool validCredentials =
            request.Username == "employer" &&
            request.Password == "password123";

        if (!validCredentials)
            return Unauthorized(new { message = "Invalid credentials." });

        var secretKey   = _configuration["Jwt:SecretKey"]!;
        var key         = new SymmetricSecurityKey(
                              Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(
                              key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, request.Username),
            new Claim(ClaimTypes.Role, "Employer")
        };

        var token = new JwtSecurityToken(
            claims:             claims,
            expires:            DateTime.UtcNow.AddHours(2),
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return Ok(new LoginResponse(tokenString));
    }

    // GET /api/auth/me
    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var username = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        var role     = User.FindFirstValue(ClaimTypes.Role);

        return Ok(new { Username = username, Role = role });
    }
}