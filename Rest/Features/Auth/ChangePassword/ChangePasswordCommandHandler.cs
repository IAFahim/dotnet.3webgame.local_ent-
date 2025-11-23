using MediatR;
using Microsoft.AspNetCore.Identity;
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
        var user = await userManager.FindByIdAsync(request.UserId);
        if (user is null)
        {
            return Result.Failure(new Error("Auth.UserNotFound", "User not found"));
        }

        var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);

        if (!result.Succeeded)
        {
            var errorMsg = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result.Failure(new Error("Auth.ChangePasswordFailed", errorMsg));
        }

        logger.LogInformation("Password changed successfully for user {Username}", user.UserName);
        return Result.Success();
    }
}