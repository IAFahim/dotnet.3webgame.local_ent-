using System.IdentityModel.Tokens.Jwt;
using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Rest.Data;
using Rest.Models;
using Rest.Services;

namespace Rest.Features.Auth.RefreshToken;

public class RefreshTokenEndpoint(
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext dbContext,
    ITokenService tokenService,
    ILogger<RefreshTokenEndpoint> logger)
    : Endpoint<RefreshTokenRequest, AuthResponse>
{
    public override void Configure()
    {
        Post("/api/v1/auth/refresh");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Refresh an expired access token";
            s.Description = "Exchange an expired access token and valid refresh token for new tokens";
            s.Response<AuthResponse>(200, "New tokens generated successfully");
            s.Response(401, "Invalid or expired tokens");
        });
    }

    public override async Task HandleAsync(RefreshTokenRequest req, CancellationToken ct)
    {
        var principal = tokenService.GetPrincipalFromExpiredToken(req.AccessToken);
        if (principal is null)
        {
            ThrowError("Invalid access token", 401);
        }

        var userId = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        var user = await userManager.Users
            .Include(u => u.RefreshTokens)
            .SingleOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null)
        {
            ThrowError("User not found", 401);
        }

        var storedToken = user.RefreshTokens.FirstOrDefault(x => x.Token == req.RefreshToken);

        if (storedToken is null)
        {
            ThrowError("Invalid refresh token", 401);
        }

        if (storedToken.Revoked != null)
        {
            logger.LogCritical("Reuse of revoked token detected for user {User}. Revoking all sessions.", user.Id);
            foreach (var token in user.RefreshTokens)
            {
                token.Revoked = DateTime.UtcNow;
            }

            dbContext.Entry(user).State = EntityState.Modified;
            await dbContext.SaveChangesAsync(ct);
            ThrowError("Security alert. Log in again.", 401);
        }

        if (!storedToken.IsActive)
        {
            ThrowError("Refresh token expired", 401);
        }

        var newRefreshToken = tokenService.GenerateRefreshToken();
        storedToken.Revoked = DateTime.UtcNow;
        storedToken.ReplacedByToken = newRefreshToken.Token;

        user.RefreshTokens.Add(newRefreshToken);

        var newAccessToken = tokenService.GenerateJwtToken(user);

        dbContext.Entry(user).State = EntityState.Modified;
        await dbContext.SaveChangesAsync(ct);

        await SendAsync(new AuthResponse(
            newAccessToken,
            newRefreshToken.Token,
            newRefreshToken.Expires,
            user.UserName!,
            user.Email!
        ), cancellation: ct);
    }
}
