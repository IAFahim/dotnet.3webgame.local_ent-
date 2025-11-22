using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Rest.Common;
using Rest.Models;

namespace Rest.Services;

public sealed class AuthService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ILogger<AuthService> logger) : IAuthService
{
    public async Task<Result<ApplicationUser>> RegisterAsync(RegisterRequest request)
    {
        logger.LogInformation("Attempting to register user: {Username}", request.Username);

        var user = new ApplicationUser
        {
            UserName = request.Username,
            Email = request.Email
        };

        var result = await userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            var errorMsg = string.Join(", ", result.Errors.Select(e => e.Description));
            logger.LogWarning("Registration failed for {Username}: {Errors}", request.Username, errorMsg);
            return Result.Failure<ApplicationUser>(new Error("Auth.RegisterFailed", errorMsg));
        }

        return user;
    }

    public async Task<Result<ApplicationUser>> LoginAsync(LoginRequest request)
    {
        var user = await userManager.FindByNameAsync(request.Username);
        
        if (user is null)
        {
            logger.LogWarning("Login failed: User {Username} not found", request.Username);
            return Result.Failure<ApplicationUser>(new Error("Auth.UserNotFound", "Invalid credentials"));
        }

        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, false);

        if (!result.Succeeded)
        {
            logger.LogWarning("Login failed: Invalid password for {Username}", request.Username);
            return Result.Failure<ApplicationUser>(new Error("Auth.InvalidCredentials", "Invalid credentials"));
        }

        user.LastLoginAt = DateTime.UtcNow;
        await userManager.UpdateAsync(user);

        return user;
    }

    public async Task<Result> ChangePasswordAsync(string userId, ChangePasswordRequest request)
    {
        var user = await userManager.FindByIdAsync(userId);
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