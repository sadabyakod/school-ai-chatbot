using Microsoft.AspNetCore.Mvc;
using SchoolAiChatbotBackend.Models;
using SchoolAiChatbotBackend.Services;
using System.Security.Cryptography;
using System.Text;

namespace SchoolAiChatbotBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthSimpleController : ControllerBase
{
    private readonly InMemoryDataService _dataService;
    private readonly JwtService _jwtService;

    public AuthSimpleController(InMemoryDataService dataService, JwtService jwtService)
    {
        _dataService = dataService;
        _jwtService = jwtService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] AuthRequest req)
    {
        if (string.IsNullOrEmpty(req.Email) || string.IsNullOrEmpty(req.Password))
            return BadRequest("Email and password are required");

        if (await _dataService.GetUserByEmailAsync(req.Email) != null)
            return BadRequest("Email already registered");

        var user = new User
        {
            Email = req.Email,
            PasswordHash = HashPassword(req.Password),
            Name = req.Email.Split('@')[0],
            Role = "User",
            SchoolId = 1
        };

        await _dataService.CreateUserAsync(user);
        var token = _jwtService.GenerateToken(user);
        
        return Ok(new AuthResponse { Token = token });
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] AuthRequest req)
    {
        if (string.IsNullOrEmpty(req.Email) || string.IsNullOrEmpty(req.Password))
            return BadRequest("Email and password are required");

        var user = await _dataService.GetUserByEmailAsync(req.Email);
        if (user == null || user.PasswordHash != HashPassword(req.Password))
            return Unauthorized("Invalid credentials");

        var token = _jwtService.GenerateToken(user);
        return Ok(new AuthResponse { Token = token });
    }

    [HttpOptions("{action?}")]
    public IActionResult Options()
    {
        return Ok();
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "salt"));
        return Convert.ToBase64String(hashedBytes);
    }
}