using Microsoft.AspNetCore.Identity;

namespace Rest.Models;

public class ApplicationUser : IdentityUser
{
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }

    public List<RefreshToken> RefreshTokens { get; set; } = [];
}