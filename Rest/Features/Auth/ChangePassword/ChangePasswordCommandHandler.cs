using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Rest.Common;
using Rest.Models;

namespace Rest.Features.Auth.ChangePassword;

public sealed class ChangePasswordCommandHandler(
    UserManager<ApplicationUser> userManager,
    ILogger<ChangePasswordCommandHandler> logger)
    : IRequestHandler<ChangePasswordCommand, Result>
{
    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        // 1. Find user
        var user = await userManager.FindByIdAsync(request.UserId);
        if (user is null)
        {
            return Result.Failure(new Error("Auth.UserNotFound", "User not found"));
        }

        // 2. Change Password (Identity handles checking CurrentPassword)
        var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);

        if (!result.Succeeded)
        {
            var errorMsg = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result.Failure(new Error("Auth.ChangePasswordFailed", errorMsg));
        }

        // 3. Critical: Revoke Refresh Tokens on password change
        // We need to load them first if they aren't loaded (FindByIdAsync doesn't include Owned Types by default usually, strictly speaking)
        // However, Owned Types are often auto-included. To be safe:
        var userWithTokens = await userManager.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (userWithTokens != null)
        {
            foreach (var token in userWithTokens.RefreshTokens.Where(t => t.IsActive))
            {
                token.Revoked = DateTime.UtcNow;
            }

            await userManager.UpdateAsync(userWithTokens);
        }

        logger.LogInformation("Password changed successfully for user {Username}", user.UserName);
        return Result.Success();
    }
}
