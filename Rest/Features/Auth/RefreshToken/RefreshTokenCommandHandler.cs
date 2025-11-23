using System.IdentityModel.Tokens.Jwt;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Rest.Common;
using Rest.Data;
using Rest.Models;
using Rest.Services;

namespace Rest.Features.Auth.RefreshToken;

public sealed class RefreshTokenCommandHandler(
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext dbContext,
    ITokenService tokenService,
    ILogger<RefreshTokenCommandHandler> logger)
    : IRequestHandler<RefreshTokenCommand, Result<AuthResponse>>
{
    public async Task<Result<AuthResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var principal = tokenService.GetPrincipalFromExpiredToken(request.AccessToken);
        if (principal is null)
        {
            return Result.Failure<AuthResponse>(new Error("Auth.InvalidToken", "Invalid access token"));
        }

        // Because we cleared the map, the ID is in 'sub'
        var userId = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        var user = await userManager.Users
            .Include(u => u.RefreshTokens)
            .SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            return Result.Failure<AuthResponse>(new Error("Auth.UserNotFound", "User not found"));
        }

        var storedToken = user.RefreshTokens.FirstOrDefault(x => x.Token == request.RefreshToken);

        if (storedToken is null)
        {
            return Result.Failure<AuthResponse>(new Error("Auth.InvalidToken", "Invalid refresh token"));
        }

        // Reuse Detection logic
        if (storedToken.Revoked != null)
        {
            logger.LogCritical("Reuse of revoked token detected for user {User}. Revoking all sessions.", user.Id);
            foreach (var token in user.RefreshTokens)
            {
                token.Revoked = DateTime.UtcNow;
            }
            dbContext.Entry(user).State = EntityState.Modified;
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result.Failure<AuthResponse>(new Error("Auth.SecurityAlert", "Security alert. Log in again."));
        }

        if (!storedToken.IsActive)
        {
            return Result.Failure<AuthResponse>(new Error("Auth.ExpiredToken", "Refresh token expired"));
        }

        // Rotation
        var newRefreshToken = tokenService.GenerateRefreshToken();
        storedToken.Revoked = DateTime.UtcNow;
        storedToken.ReplacedByToken = newRefreshToken.Token;

        user.RefreshTokens.Add(newRefreshToken);

        var newAccessToken = tokenService.GenerateJwtToken(user);

        dbContext.Entry(user).State = EntityState.Modified;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            newAccessToken,
            newRefreshToken.Token,
            newRefreshToken.Expires,
            user.UserName!,
            user.Email!);
    }
}
