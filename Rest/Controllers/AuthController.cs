using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Rest.Common;
using Rest.Features.Auth.ChangePassword;
using Rest.Features.Auth.Login;
using Rest.Features.Auth.Register;
using Rest.Models;

namespace Rest.Controllers;

[ApiController]
[Route("api/v1/auth")] 
[EnableRateLimiting("fixed")] 
public class AuthController(
    ISender sender,
    SignInManager<ApplicationUser> signInManager) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] Dots request)
    {
        // Map DTO to Command
        var command = new RegisterCommand(request.Username, request.Email, request.Password);
        
        var result = await sender.Send(command);

        if (result.IsFailure)
        {
            return BadRequest(new ProblemDetails { Title = "Registration Failed", Detail = result.Error.Description });
        }

        return Ok(result.Value);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var command = new LoginCommand(request.Username, request.Password);
        
        var result = await sender.Send(command);

        if (result.IsFailure)
        {
            return Unauthorized(new ProblemDetails { Title = "Auth Failed", Detail = result.Error.Description });
        }

        return Ok(result.Value);
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

        var command = new ChangePasswordCommand(
            userId, 
            request.CurrentPassword, 
            request.NewPassword, 
            request.ConfirmNewPassword);

        var result = await sender.Send(command);

        if (result.IsFailure)
        {
            return BadRequest(new ProblemDetails { Title = "Operation Failed", Detail = result.Error.Description });
        }

        return Ok(new { message = "Password changed successfully" });
    }
}
