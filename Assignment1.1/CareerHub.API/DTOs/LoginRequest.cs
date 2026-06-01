namespace CareerHub.API.DTOs;

// What the client sends to login
// Username + Password — that is all we need
public record LoginRequest(string Username, string Password);