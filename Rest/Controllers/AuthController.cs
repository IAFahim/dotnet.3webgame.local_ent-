using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rest.Extensions;
using Rest.Features.Auth.ChangePassword;
using Rest.Features.Auth.Login;
using Rest.Features.Auth.Logout; // <--- Ensure this is here
using Rest.Features.Auth.RefreshToken;
using Rest.Features.Auth.Register;
using Rest.Models;
using LoginRequest = Rest.Features.Auth.Login.LoginRequest;
using RegisterRequest = Rest.Features.Auth.Register.RegisterRequest;

namespace Rest.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(ISender sender) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var command = new RegisterCommand(request.Username, request.Email, request.Password);
        var result = await sender.Send(command);
        return result.IsFailure ? Problem(result.Error) : Ok(result.Value);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var command = new LoginCommand(request.Username, request.Password);
        var result = await sender.Send(command);
        return result.IsFailure ? Unauthorized(ToProblem(result.Error)) : Ok(result.Value);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        var command = new RefreshTokenCommand(request.AccessToken, request.RefreshToken);
        var result = await sender.Send(command);
        return result.IsFailure ? Unauthorized(ToProblem(result.Error)) : Ok(result.Value);
    }
    
    
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        // No try-catch. Let specific errors bubble up.
        var userId = User.GetUserId(); 
        
        var command = new ChangePasswordCommand(
            userId, 
            request.CurrentPassword, 
            request.NewPassword, 
            request.ConfirmNewPassword);

        var result = await sender.Send(command);

        // Return specific error if logic fails (e.g., wrong current password)
        return result.IsFailure ? BadRequest(ToProblem(result.Error)) : Ok(new { message = "Password changed successfully" });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var userId = User.GetUserId();
        var command = new LogoutCommand(userId);
        await sender.Send(command);
        
        return Ok(new { message = "Logged out successfully." });
    }
    
    private static ProblemDetails ToProblem(Rest.Common.Error error) =>
        new() { Title = error.Code, Detail = error.Description, Type = error.Code };
    
    private ObjectResult Problem(Rest.Common.Error error) =>
        BadRequest(ToProblem(error));
}

