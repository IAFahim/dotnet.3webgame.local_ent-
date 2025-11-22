using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Rest.Models;
using Rest.Services;

namespace Rest.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
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
    /// Registers a new user.
    /// </summary>
    /// <param name="model">The registration details.</param>
    /// <returns>The created user details and auth token.</returns>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterDto model)
    {
        var (success, user, errors) = await _userService.RegisterAsync(model);

        if (!success)
        {
            return BadRequest(new ProblemDetails 
            { 
                Title = "Registration Failed",
                Detail = "One or more validation errors occurred.",
                Extensions = { ["errors"] = errors }
            });
        }

        var token = _tokenService.GenerateJwtToken(user!);

        var response = new AuthResponseDto
        {
            Token = token,
            Expiration = DateTime.UtcNow.AddHours(2),
            Username = user!.UserName!,
            Email = user.Email!,
            CoinBalance = user.CoinBalance
        };

        return CreatedAtAction(nameof(GetProfile), response);
    }

    /// <summary>
    /// Logs in an existing user.
    /// </summary>
    /// <param name="model">The login credentials.</param>
    /// <returns>Auth token and user details.</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginDto model)
    {
        var (success, user) = await _userService.LoginAsync(model);

        if (!success || user == null)
        {
            return Unauthorized(new ProblemDetails { Title = "Authentication Failed", Detail = "Invalid username or password." });
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
    /// Logs out the current user (Cookie based).
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return Ok(new { message = "Logged out successfully" });
    }

    /// <summary>
    /// Retrieves the current user's profile.
    /// </summary>
    [HttpGet("profile")]
    [Authorize]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)] // Replace object with a DTO class ideally
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null) return NotFound();

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
    /// Retrieves current coin balance.
    /// </summary>
    [HttpGet("coins")]
    [Authorize]
    [ProducesResponseType(typeof(CoinBalanceDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCoins()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null) return NotFound();

        return Ok(new CoinBalanceDto
        {
            Username = user.UserName!,
            CoinBalance = user.CoinBalance
        });
    }
}