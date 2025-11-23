using Microsoft.EntityFrameworkCore;

namespace Rest.Models;

[Owned]
public class RefreshToken
{
    public int Id { get; set; } // Needed for SQLite PK generation in owned types
    public string Token { get; set; } = string.Empty;
    public DateTime Expires { get; set; }
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public DateTime? Revoked { get; set; }
    public string? ReplacedByToken { get; set; }

    public bool IsActive => Revoked == null && DateTime.UtcNow <= Expires;
}
