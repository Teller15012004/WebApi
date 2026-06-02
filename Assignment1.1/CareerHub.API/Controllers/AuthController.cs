// Controllers/AuthController.cs

[ApiController]   // enables automatic 400 for validation failures
[Route("api/auth")] // all endpoints here start with /api/auth
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;

    // IConfiguration is injected automatically by ASP.NET
    // Gives us access to appsettings.Development.json
    public AuthController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpPost("login")] // POST /api/auth/login
    public IActionResult Login([FromBody] LoginRequest request)
    {
        // Step 1 — Check credentials
        // Hardcoded for now — Week 2 replaces with database lookup
        bool validCredentials =
            request.Username == "employer" &&
            request.Password == "password123";

        if (!validCredentials)
        {
            // Return 401 — DO NOT say which field was wrong
            // "Wrong password" tells attacker the username is correct
            // "Invalid credentials" reveals nothing useful
            return Unauthorized(new { message = "Invalid credentials." });
        }

        // Step 2 — Build the signing key
        var secretKey   = _configuration["Jwt:SecretKey"]!;
        var key         = new SymmetricSecurityKey(
                              Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(
                              key,
                              SecurityAlgorithms.HmacSha256);
        //                                      ↑
        //                    HMAC-SHA256 = the signing algorithm
        //                    same algorithm must be used to verify

        // Step 3 — Define claims
        // Claims = the payload inside the token
        // Think of it as fields on an ID card
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, request.Username),
            //         ↑                            ↑
            //    "sub" field                  value = "employer"

            new Claim(ClaimTypes.Role, "Employer")
            //         ↑               ↑
            //    "role" field      value = "Employer"
            //    This is what [Authorize(Roles = "Employer")] checks
        };

        // Step 4 — Build the token
        var token = new JwtSecurityToken(
            claims:             claims,
            expires:            DateTime.UtcNow.AddHours(2),
            //                  ↑ token expires 2 hours from now
            signingCredentials: credentials
        );

        // Step 5 — Serialise to a string
        // JwtSecurityTokenHandler converts the token object
        // into the eyJhbGci... string the client receives
        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return Ok(new LoginResponse(tokenString));
    }
}