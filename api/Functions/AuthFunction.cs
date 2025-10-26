using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using SchoolAiChatbot.Models;
using SchoolAiChatbot.Services;

namespace SchoolAiChatbot.Functions;

public class AuthFunction
{
    private readonly ILogger<AuthFunction> _logger;
    private readonly AuthService _authService;
    private readonly CosmosDbService _dbService;

    public AuthFunction(ILogger<AuthFunction> logger)
    {
        _logger = logger;
        _authService = new AuthService();
        _dbService = new CosmosDbService();
    }

    [Function("register")]
    public async Task<HttpResponseData> Register(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/register")] HttpRequestData req)
    {
        _logger.LogInformation("Processing user registration");

        try
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var authRequest = JsonSerializer.Deserialize<AuthRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (authRequest == null || string.IsNullOrWhiteSpace(authRequest.Email) || string.IsNullOrWhiteSpace(authRequest.Password))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync(JsonSerializer.Serialize(new { error = "Email and password are required" }));
                return badResponse;
            }

            // Check if user already exists
            var existingUser = await _dbService.GetUserByEmailAsync(authRequest.Email);
            if (existingUser != null)
            {
                var conflictResponse = req.CreateResponse(HttpStatusCode.Conflict);
                await conflictResponse.WriteStringAsync(JsonSerializer.Serialize(new { error = "Email already registered" }));
                return conflictResponse;
            }

            // Create new user
            var user = new User
            {
                Email = authRequest.Email,
                PasswordHash = _authService.HashPassword(authRequest.Password),
                Name = authRequest.Email.Split('@')[0],
                Role = "User"
            };

            await _dbService.CreateUserAsync(user);

            // Generate JWT token
            var token = _authService.GenerateJwtToken(user);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");

            var authResponse = new AuthResponse
            {
                Token = token,
                Message = "Registration successful"
            };

            await response.WriteStringAsync(JsonSerializer.Serialize(authResponse));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing registration");
            
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync(JsonSerializer.Serialize(new { error = "Registration failed" }));
            return errorResponse;
        }
    }

    [Function("login")]
    public async Task<HttpResponseData> Login(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/login")] HttpRequestData req)
    {
        _logger.LogInformation("Processing user login");

        try
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var authRequest = JsonSerializer.Deserialize<AuthRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (authRequest == null || string.IsNullOrWhiteSpace(authRequest.Email) || string.IsNullOrWhiteSpace(authRequest.Password))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync(JsonSerializer.Serialize(new { error = "Email and password are required" }));
                return badResponse;
            }

            // Find user
            var user = _users.FirstOrDefault(u => u.Email == authRequest.Email);
            if (user == null || !_authService.VerifyPassword(authRequest.Password, user.PasswordHash))
            {
                var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                await unauthorizedResponse.WriteStringAsync(JsonSerializer.Serialize(new { error = "Invalid credentials" }));
                return unauthorizedResponse;
            }

            // Generate JWT token
            var token = _authService.GenerateJwtToken(user);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");

            var authResponse = new AuthResponse
            {
                Token = token,
                Message = "Login successful"
            };

            await response.WriteStringAsync(JsonSerializer.Serialize(authResponse));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing login");
            
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync(JsonSerializer.Serialize(new { error = "Login failed" }));
            return errorResponse;
        }
    }

    [Function("auth-options")]
    public HttpResponseData OptionsAuth(
        [HttpTrigger(AuthorizationLevel.Anonymous, "options", Route = "auth/{action?}")] HttpRequestData req)
    {
        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Access-Control-Allow-Origin", "*");
        response.Headers.Add("Access-Control-Allow-Methods", "POST, OPTIONS");
        response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");
        return response;
    }
}