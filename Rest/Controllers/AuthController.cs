using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Rest.Models;
using Rest.Services;

namespace Rest.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ILogger<AuthController> _logger;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ITokenService _tokenService;
    private readonly IUserService _userService;

    public AuthController(
        IUserService userService,
        ITokenService tokenService,
        SignInManager<ApplicationUser> signInManager,
        ILogger<AuthController> logger)
    {
        _userService = userService;
        _tokenService = tokenService;
        _signInManager = signInManager;
        _logger = logger;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterDto model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        _logger.LogInformation("Attempting to register user: {Username}", model.Username);

        var (success, user, errors) = await _userService.RegisterAsync(model);

        if (!success)
        {
            _logger.LogWarning("Registration failed for {Username}: {Errors}", model.Username,
                string.Join(", ", errors));
            return BadRequest(new { message = "Registration failed", errors });
        }

        _logger.LogInformation("User {Username} registered successfully.", model.Username);

        var token = _tokenService.GenerateJwtToken(user!);

        return Ok(new AuthResponseDto
        {
            Token = token,
            Expiration = DateTime.UtcNow.AddHours(2),
            Username = user!.UserName!,
            Email = user.Email!,
            CoinBalance = user.CoinBalance
        });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDto model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        _logger.LogInformation("Login attempt for user: {Username}", model.Username);

        var (success, user) = await _userService.LoginAsync(model);

        if (!success || user == null)
        {
            _logger.LogWarning("Invalid login attempt for user: {Username}", model.Username);
            return Unauthorized(new { message = "Invalid credentials" });
        }

        _logger.LogInformation("User {Username} logged in successfully.", model.Username);

        var token = _tokenService.GenerateJwtToken(user);

        return Ok(new AuthResponseDto
        {
            Token = token,
            Expiration = DateTime.UtcNow.AddHours(2),
            Username = user.UserName!,
            Email = user.Email!,
            CoinBalance = user.CoinBalance
        });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var username = User.FindFirstValue(ClaimTypes.Name);
        await _signInManager.SignOutAsync();

        _logger.LogInformation("User {Username} logged out successfully.", username);
        return Ok(new { message = "Logged out successfully" });
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var username = User.FindFirstValue(ClaimTypes.Name);

        if (userId == null) return Unauthorized();

        var (success, errors) =
            await _userService.ChangePasswordAsync(userId, model.CurrentPassword, model.NewPassword);

        if (!success)
        {
            _logger.LogWarning("Password change failed for user {Username}: {Errors}", username,
                string.Join(", ", errors));
            return BadRequest(new { message = "Failed to change password", errors });
        }

        _logger.LogInformation("Password changed successfully for user {Username}", username);
        return Ok(new { message = "Password changed successfully" });
    }
}