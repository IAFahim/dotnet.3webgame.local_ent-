using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Rest.Common;
using Rest.Models;
using Rest.Options;
using Rest.Services;

namespace Rest.Features.Auth.Login;

public sealed class LoginCommandHandler(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ITokenService tokenService,
    IOptions<JwtSettings> jwtSettings,
    TimeProvider timeProvider,
    ILogger<LoginCommandHandler> logger) 
    : IRequestHandler<LoginCommand, Result<AuthResponse>>
{
    private readonly JwtSettings _settings = jwtSettings.Value;

    public async Task<Result<AuthResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByNameAsync(request.Username);
        
        if (user is null)
        {
            // We use a generic message to avoid user enumeration attacks
            logger.LogWarning("Login failed: User {Username} not found", request.Username);
            return Result.Failure<AuthResponse>(new Error("Auth.InvalidCredentials", "Invalid credentials"));
        }

        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, false);

        if (!result.Succeeded)
        {
            logger.LogWarning("Login failed: Invalid password for {Username}", request.Username);
            return Result.Failure<AuthResponse>(new Error("Auth.InvalidCredentials", "Invalid credentials"));
        }

        // Update audit fields using testable time
        user.LastLoginAt = timeProvider.GetUtcNow().DateTime;
        await userManager.UpdateAsync(user);

        var token = tokenService.GenerateJwtToken(user);
        var expiration = timeProvider.GetUtcNow().AddHours(_settings.ExpirationHours).DateTime;

        return new AuthResponse(token, expiration, user.UserName!, user.Email!);
    }
}