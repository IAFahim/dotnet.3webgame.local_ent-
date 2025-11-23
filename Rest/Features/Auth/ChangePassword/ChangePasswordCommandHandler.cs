using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Rest.Common;
using Rest.Data;
using Rest.Models;

namespace Rest.Features.Auth.ChangePassword;

public sealed class ChangePasswordCommandHandler(
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext dbContext,
    ILogger<ChangePasswordCommandHandler> logger)
    : IRequestHandler<ChangePasswordCommand, Result>
{
    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        // 1. Load user with tracking enabled and include refresh tokens
        var user = await userManager.Users
            .Include(u => u.RefreshTokens)
            .AsTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
            
        if (user is null)
        {
            return Result.Failure(new Error("Auth.UserNotFound", "User not found"));
        }

        // 2. Change Password (Identity handles checking CurrentPassword)
        var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            logger.LogWarning("Failed to change password for user {Username}: {Errors}", user.UserName, errors);
            return Result.Failure(new Error("Auth.ChangePasswordFailed", errors));
        }

        // 3. Revoke all active refresh tokens on password change
        foreach (var token in user.RefreshTokens.Where(t => t.IsActive))
        {
            token.Revoked = DateTime.UtcNow;
        }
        
        // Update user with revoked tokens
        dbContext.Entry(user).State = EntityState.Modified;
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Password changed successfully for user {Username}", user.UserName);
        return Result.Success();
    }
}
