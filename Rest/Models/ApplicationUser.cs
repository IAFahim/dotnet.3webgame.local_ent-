using Microsoft.AspNetCore.Identity;

namespace Rest.Models;

/// <summary>
///     Custom user class extending IdentityUser with game-specific properties
/// </summary>
public class ApplicationUser : IdentityUser
{
    public decimal CoinBalance { get; set; } = 100.00m;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
}