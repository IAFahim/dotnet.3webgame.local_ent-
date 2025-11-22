using Microsoft.AspNetCore.Identity;
using Rest.Models;

namespace Rest.Services;

/// <summary>
///     Service for user-related operations with ASP.NET Core Identity
/// </summary>
public class UserService : IUserService
{
    private readonly ILogger<UserService> _logger;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public UserService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<UserService> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    public async Task<(bool Success, ApplicationUser? User, IEnumerable<string> Errors)> RegisterAsync(
        RegisterDto model)
    {
        var user = new ApplicationUser
        {
            UserName = model.Username,
            Email = model.Email,
            CoinBalance = 100.00m,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (!result.Succeeded) return (false, null, result.Errors.Select(e => e.Description));

        _logger.LogInformation("User {Username} registered successfully", user.UserName);
        return (true, user, Enumerable.Empty<string>());
    }

    public async Task<(bool Success, ApplicationUser? User)> LoginAsync(LoginDto model)
    {
        var user = await _userManager.FindByNameAsync(model.Username);
        if (user == null)
        {
            _logger.LogWarning("Login attempt for non-existent user {Username}", model.Username);
            return (false, null);
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);

        if (!result.Succeeded)
        {
            _logger.LogWarning("Failed login attempt for user {Username}", model.Username);
            return (false, null);
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        _logger.LogInformation("User {Username} logged in successfully", user.UserName);
        return (true, user);
    }

    public async Task<ApplicationUser?> GetUserByIdAsync(string userId)
    {
        return await _userManager.FindByIdAsync(userId);
    }

    public async Task<bool> UpdateUserAsync(ApplicationUser user)
    {
        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded;
    }

    public async Task<(bool Success, IEnumerable<string> Errors)> ChangePasswordAsync(
        string userId, string currentPassword, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return (false, new[] { "User not found" });

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);

        if (!result.Succeeded) return (false, result.Errors.Select(e => e.Description));

        _logger.LogInformation("User {Username} changed password", user.UserName);
        return (true, Enumerable.Empty<string>());
    }
}