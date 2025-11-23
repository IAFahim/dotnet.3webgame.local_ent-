using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Rest.Data;
using Rest.Extensions;
using Rest.Models;

namespace Rest.Features.Auth.ChangePassword;

public class ChangePasswordEndpoint(
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext dbContext,
    ILogger<ChangePasswordEndpoint> logger)
    : Endpoint<ChangePasswordRequest>
{
    public override void Configure()
    {
        Post("/api/v1/auth/change-password");
        Summary(s =>
        {
            s.Summary = "Change password for authenticated user";
            s.Description = "Change the password for the currently authenticated user";
            s.Response(200, "Password changed successfully");
            s.Response(400, "Invalid password or validation failed");
            s.Response(401, "Unauthorized");
        });
    }

    public override async Task HandleAsync(ChangePasswordRequest req, CancellationToken ct)
    {
        var userId = User.GetUserId();

        var user = await userManager.Users
            .Include(u => u.RefreshTokens)
            .AsTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null)
        {
            ThrowError("User not found", 400);
        }

        var result = await userManager.ChangePasswordAsync(user, req.CurrentPassword, req.NewPassword);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            logger.LogWarning("Failed to change password for user {Username}: {Errors}", user.UserName, errors);
            ThrowError(errors, 400);
        }

        foreach (var token in user.RefreshTokens.Where(t => t.IsActive))
        {
            token.Revoked = DateTime.UtcNow;
        }

        dbContext.Entry(user).State = EntityState.Modified;
        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation("Password changed successfully for user {Username}", user.UserName);

        await SendAsync(new { message = "Password changed successfully" }, cancellation: ct);
    }
}
