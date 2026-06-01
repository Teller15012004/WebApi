using Microsoft.AspNetCore.Mvc;
using API.Models;
using Microsoft.IdentiyModel.Tokens;
using System.IdentiyModel.Tokens.Jwt;
using System.Security.Claims;
using API.DTOs;
using API.Exceptions;

namespace API.Controllers;
[Route("CareerHub.API/[controller]")]

public class AuthController : ControllerBase
{
    [HttpPost("login")]
     public IActionResult Login([FromBody] LoginRequest request)
    {
        
    }
}