using System.Security.Claims;
using FileProcessing.API.Services;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace FileProcessing.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(ITokenService tokenService, ILogger<AuthController> logger)
    {
        _tokenService = tokenService;
        _logger = logger;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest req)
    {
        if (req.Username == "pablo" && req.Password == "123")
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, req.Username),
                new Claim(ClaimTypes.Role, "User")
            };
            Log.Information("Login Realizado por : " + req.Username );
            var token = _tokenService.GenerateToken(req.Username, claims);
            return Ok(new { access_token = token, token_type = "Bearer" });
        }

        return Unauthorized();
    }
}

public record LoginRequest(string Username, string Password);
