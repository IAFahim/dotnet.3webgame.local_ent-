using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Rest.Common;
using Rest.Data;
using Rest.Models;
using Rest.Services;

namespace Rest.Features.Auth.Login;

public sealed class LoginCommandHandler(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ApplicationDbContext dbContext,
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

        // Reload user with RefreshTokens to ensure we have the collection
        user = await userManager.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == user.Id, cancellationToken) ?? user;

        var accessToken = tokenService.GenerateJwtToken(user);
        var refreshToken = tokenService.GenerateRefreshToken();

        user.RefreshTokens.Add(refreshToken);

        var expiredTokens = user.RefreshTokens
            .Where(t => !t.IsActive && t.Created.AddDays(2) <= timeProvider.GetUtcNow().DateTime)
            .ToList();
        
        foreach (var token in expiredTokens)
        {
            user.RefreshTokens.Remove(token);
        }

        user.LastLoginAt = timeProvider.GetUtcNow().DateTime;

        // Explicitly save changes to the DbContext to ensure owned entities are persisted
        dbContext.Entry(user).State = EntityState.Modified;
        await dbContext.SaveChangesAsync(cancellationToken);

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
