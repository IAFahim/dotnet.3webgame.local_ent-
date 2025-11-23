using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Rest.Data;
using Rest.Extensions;
using Rest.Models;

namespace Rest.Features.Auth.Logout;

public class LogoutEndpoint(
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext dbContext,
    ILogger<LogoutEndpoint> logger)
    : EndpointWithoutRequest
{
    public override void Configure()
    {
        Post("/api/v1/auth/logout");
        Summary(s =>
        {
            s.Summary = "Logout and revoke all sessions";
            s.Description = "Revoke all active refresh tokens for the authenticated user";
            s.Response(200, "Logged out successfully");
            s.Response(401, "Unauthorized");
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.GetUserId();

        var user = await userManager.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is not null)
        {
            foreach (var token in user.RefreshTokens.Where(t => t.IsActive))
            {
                token.Revoked = DateTime.UtcNow;
            }

            dbContext.Entry(user).State = EntityState.Modified;
            await dbContext.SaveChangesAsync(ct);

            logger.LogInformation("User {Username} logged out and all sessions revoked.", user.UserName);
        }

        await SendAsync(new { message = "Logged out successfully" }, cancellation: ct);
    }
}
