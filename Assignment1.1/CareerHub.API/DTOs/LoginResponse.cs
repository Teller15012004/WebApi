namespace CareerHub.API.DTOs;

// What the server sends back after a successful login
// Just the token string — client stores this and sends it with every request
public record LoginResponse(string Token);