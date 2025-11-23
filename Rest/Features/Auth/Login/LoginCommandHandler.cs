using MediatR;
using Microsoft.AspNetCore.Identity;
using Rest.Common;
using Rest.Models;
using Rest.Services;

namespace Rest.Features.Auth.Login;

public sealed class LoginCommandHandler(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ITokenService tokenService,
    TimeProvider timeProvider,
    ILogger<LoginCommandHandler> logger)
    : IRequestHandler<LoginCommand, Result<AuthResponse>>
{
    public async Task<Result<AuthResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByNameAsync(request.Username);
        if (user is null)
        {
            return InvalidCreds();
        }

        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, true);
        if (!result.Succeeded)
        {
            if (result.IsLockedOut)
            {
                logger.LogWarning("User {Username} is locked out", request.Username);
                return Result.Failure<AuthResponse>(new Error("Auth.LockedOut",
                    "Account is locked out. Please try again later."));
            }

            return InvalidCreds();
        }

        var accessToken = tokenService.GenerateJwtToken(user);
        var refreshToken = tokenService.GenerateRefreshToken();

        user.RefreshTokens.Add(refreshToken);

        user.RefreshTokens.RemoveAll(t =>
            !t.IsActive &&
            t.Created.AddDays(2) <= timeProvider.GetUtcNow().DateTime);

        user.LastLoginAt = timeProvider.GetUtcNow().DateTime;

        await userManager.UpdateAsync(user);

        logger.LogInformation("User {Username} logged in successfully", user.UserName);

        return new AuthResponse(
            accessToken,
            refreshToken.Token,
            refreshToken.Expires,
            user.UserName!,
            user.Email!);
    }

    private Result<AuthResponse> InvalidCreds()
    {
        logger.LogWarning("Invalid login attempt");
        return Result.Failure<AuthResponse>(new Error("Auth.InvalidCredentials", "Invalid credentials"));
    }
}
