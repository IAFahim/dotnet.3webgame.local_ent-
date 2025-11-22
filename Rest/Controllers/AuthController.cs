using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Rest.Models;
using Rest.Services;

namespace Rest.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    IAuthService authService,
    ITokenService tokenService,
    SignInManager<ApplicationUser> signInManager) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await authService.RegisterAsync(request);

        if (result.IsFailure)
        {
            return BadRequest(new ProblemDetails { Title = "Registration Failed", Detail = result.Error.Description });
        }

        var token = tokenService.GenerateJwtToken(result.Value);

        return Ok(new AuthResponse(
            token,
            DateTime.UtcNow.AddHours(2),
            result.Value.UserName!,
            result.Value.Email!
        ));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await authService.LoginAsync(request);

        if (result.IsFailure)
        {
            return Unauthorized(new ProblemDetails { Title = "Auth Failed", Detail = result.Error.Description });
        }

        var token = tokenService.GenerateJwtToken(result.Value);

        return Ok(new AuthResponse(
            token,
            DateTime.UtcNow.AddHours(2),
            result.Value.UserName!,
            result.Value.Email!
        ));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return Ok(new { message = "Logged out successfully" });
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var result = await authService.ChangePasswordAsync(userId, request);

        if (result.IsFailure)
        {
            return BadRequest(new ProblemDetails { Title = "Operation Failed", Detail = result.Error.Description });
        }

        return Ok(new { message = "Password changed successfully" });
    }
}