using System.ComponentModel.DataAnnotations;
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
    private readonly IUserService _userService;
    private readonly ITokenService _tokenService;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<AuthController> _logger;

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

    /// <summary>
    /// Register a new user account
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterDto model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var (success, user, errors) = await _userService.RegisterAsync(model);

        if (!success)
        {
            return BadRequest(new
            {
                message = "Registration failed",
                errors
            });
        }

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

    /// <summary>
    /// Login with username and password
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDto model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var (success, user) = await _userService.LoginAsync(model);

        if (!success || user == null)
        {
            return Unauthorized(new { message = "Invalid credentials" });
        }

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

    /// <summary>
    /// Logout current user
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User logged out successfully");
        return Ok(new { message = "Logged out successfully" });
    }

    /// <summary>
    /// Get current user's coin balance
    /// </summary>
    [HttpGet("coins")]
    [Authorize]
    public async Task<IActionResult> GetCoins()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null)
            return NotFound(new { message = "User not found" });

        return Ok(new CoinBalanceDto
        {
            Username = user.UserName!,
            CoinBalance = user.CoinBalance
        });
    }

    /// <summary>
    /// Get user profile information
    /// </summary>
    [HttpGet("profile")]
    [Authorize]
    public async Task<IActionResult> GetProfile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null)
            return NotFound(new { message = "User not found" });

        return Ok(new
        {
            username = user.UserName,
            email = user.Email,
            coinBalance = user.CoinBalance,
            createdAt = user.CreatedAt,
            lastLoginAt = user.LastLoginAt
        });
    }

    /// <summary>
    /// Change password for current user
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        var (success, errors) = await _userService.ChangePasswordAsync(userId, model.CurrentPassword, model.NewPassword);

        if (!success)
        {
            return BadRequest(new
            {
                message = "Failed to change password",
                errors
            });
        }

        return Ok(new { message = "Password changed successfully" });
    }
}

public class ChangePasswordDto
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 8)]
    public string NewPassword { get; set; } = string.Empty;
}
