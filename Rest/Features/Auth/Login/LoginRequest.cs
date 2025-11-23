using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Rest.Features.Auth.Login;

public sealed class LoginRequest
{
    [Required]
    [StringLength(50, MinimumLength = 3)]
    [DefaultValue("testuser")]
    public required string Username { get; init; }
    
    [Required]
    [StringLength(100, MinimumLength = 8)]
    [DefaultValue("Password123!")]
    public required string Password { get; init; }
}

