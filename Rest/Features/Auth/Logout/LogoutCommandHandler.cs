using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Rest.Common;
using Rest.Data;
using Rest.Models;

namespace Rest.Features.Auth.Logout;

public sealed class LogoutCommandHandler(
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext dbContext,
    ILogger<LogoutCommandHandler> logger)
    : IRequestHandler<LogoutCommand, Result>
{
    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Success(); // Idempotent
        }

        // Revoke all active tokens
        foreach (var token in user.RefreshTokens.Where(t => t.IsActive))
        {
            token.Revoked = DateTime.UtcNow;
        }

        dbContext.Entry(user).State = EntityState.Modified;
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {Username} logged out and all sessions revoked.", user.UserName);

        return Result.Success();
    }
}
