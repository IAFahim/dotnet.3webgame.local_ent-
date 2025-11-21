using Rest.Models;

namespace Rest.Services;

/// <summary>
/// Interface for user-related operations
/// </summary>
public interface IUserService
{
    Task<(bool Success, ApplicationUser? User, IEnumerable<string> Errors)> RegisterAsync(RegisterDto model);
    Task<(bool Success, ApplicationUser? User)> LoginAsync(LoginDto model);
    Task<ApplicationUser?> GetUserByIdAsync(string userId);
    Task<bool> UpdateUserAsync(ApplicationUser user);
    Task<(bool Success, IEnumerable<string> Errors)> ChangePasswordAsync(string userId, string currentPassword, string newPassword);
}
