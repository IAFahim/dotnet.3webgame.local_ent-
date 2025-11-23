using MediatR;
using Microsoft.AspNetCore.Identity;
using Rest.Common;
using Rest.Models;
using Rest.Services;

namespace Rest.Features.Auth.Register;

public sealed class RegisterCommandHandler(
    UserManager<ApplicationUser> userManager,
    ITokenService tokenService,
    ILogger<RegisterCommandHandler> logger) 
    : IRequestHandler<RegisterCommand, Result<AuthResponse>>
{
    public async Task<Result<AuthResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Registering user: {Username}", request.Username);

        var existingUser = await userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
        {
            return Result.Failure<AuthResponse>(new Error("Auth.DuplicateEmail", "Email is already registered."));
        }

        var user = new ApplicationUser 
        { 
            UserName = request.Username, 
            Email = request.Email,
            EmailConfirmed = false
        };

        var result = await userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            logger.LogWarning("Failed to register user {Username}: {Errors}", request.Username, errors);
            return Result.Failure<AuthResponse>(new Error("Auth.RegisterFailed", errors));
        }

        var accessToken = tokenService.GenerateJwtToken(user);
        var refreshToken = tokenService.GenerateRefreshToken();

        user.RefreshTokens.Add(refreshToken);
        await userManager.UpdateAsync(user);

        logger.LogInformation("User {Username} registered successfully", user.UserName);

        return new AuthResponse(
            accessToken, 
            refreshToken.Token, 
            refreshToken.Expires, 
            user.UserName!, 
            user.Email!
        );
    }
}