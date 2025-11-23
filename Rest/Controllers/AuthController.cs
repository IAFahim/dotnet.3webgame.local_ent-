using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rest.Extensions;
using Rest.Features.Auth;
using Rest.Features.Auth.ChangePassword;
using Rest.Features.Auth.Login;
using Rest.Features.Auth.Logout;
using Rest.Features.Auth.RefreshToken;
using Rest.Features.Auth.Register;
using LoginRequest = Rest.Features.Auth.Login.LoginRequest;
using RegisterRequest = Rest.Features.Auth.Register.RegisterRequest;

namespace Rest.Controllers;

/// <summary>
///     Authentication and authorization endpoints.
/// </summary>
[Route("api/v1/auth")]
public sealed class AuthController(ISender sender) : ApiControllerBase
{
    /// <summary>
    ///     Register a new user account.
    /// </summary>
    /// <param name="request">Registration details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authentication response with tokens</returns>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new RegisterCommand(request.Username, request.Email, request.Password);
        var result = await sender.Send(command, cancellationToken);
        return result.IsFailure ? BadRequestProblem(result.Error) : Ok(result.Value);
    }

    /// <summary>
    ///     Login with username and password.
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authentication response with tokens</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new LoginCommand(request.Username, request.Password);
        var result = await sender.Send(command, cancellationToken);
        return result.IsFailure ? UnauthorizedProblem(result.Error) : Ok(result.Value);
    }

    /// <summary>
    ///     Refresh an expired access token using a refresh token.
    /// </summary>
    /// <param name="request">Access token and refresh token</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>New authentication response with tokens</returns>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new RefreshTokenCommand(request.AccessToken, request.RefreshToken);
        var result = await sender.Send(command, cancellationToken);
        return result.IsFailure ? UnauthorizedProblem(result.Error) : Ok(result.Value);
    }

    /// <summary>
    ///     Change the password for the authenticated user.
    /// </summary>
    /// <param name="request">Current and new password</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success message</returns>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();

        var command = new ChangePasswordCommand(
            userId,
            request.CurrentPassword,
            request.NewPassword,
            request.ConfirmNewPassword);

        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure
            ? BadRequestProblem(result.Error)
            : Ok(new { message = "Password changed successfully" });
    }

    /// <summary>
    ///     Logout and revoke all active refresh tokens for the authenticated user.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success message</returns>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        var command = new LogoutCommand(userId);
        await sender.Send(command, cancellationToken);

        return Ok(new { message = "Logged out successfully" });
    }
}
