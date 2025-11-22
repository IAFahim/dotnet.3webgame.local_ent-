using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Rest.Models;

public record RegisterRequest(
    // Validation: No prefix (targets Parameter)
    [Required] 
    [StringLength(50, MinimumLength = 3)]
    // Docs: Use 'property:' (targets Property for Scalar)
    [property: DefaultValue("new_hero")] 
    string Username,

    [Required] 
    [EmailAddress] 
    [property: DefaultValue("hero@game.com")] 
    string Email,

    [Required] 
    [StringLength(100, MinimumLength = 8)] 
    [property: DefaultValue("Password123!")] 
    string Password
);

public record LoginRequest(
    [Required] 
    [property: DefaultValue("player1")] 
    string Username,

    [Required] 
    [property: DefaultValue("Player123!")] 
    string Password
);

// This one stays the same because it's a standard class/record (not positional)
public record ChangePasswordRequest
{
    [Required]
    [DefaultValue("Player123!")]
    public string CurrentPassword { get; init; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 8)]
    [DefaultValue("NewPassword123!")]
    public string NewPassword { get; init; } = string.Empty;

    [Required]
    [Compare("NewPassword")]
    [DefaultValue("NewPassword123!")]
    public string ConfirmNewPassword { get; init; } = string.Empty;
}

public record AuthResponse(
    string Token,
    DateTime Expiration,
    string Username,
    string Email
);