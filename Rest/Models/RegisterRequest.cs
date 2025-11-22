using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Rest.Models;

public record RegisterRequest(
    [property: Required] 
    [property: StringLength(50, MinimumLength = 3)] 
    [property: DefaultValue("new_hero")]
    string Username,

    [property: Required] 
    [property: EmailAddress] 
    [property: DefaultValue("hero@game.com")] 
    string Email,

    [property: Required] 
    [property: StringLength(100, MinimumLength = 8)] 
    [property: DefaultValue("Password123!")] 
    string Password
);

public record LoginRequest(
    [property: Required] 
    [property: DefaultValue("player1")]
    string Username,

    [property: Required] 
    [property: DefaultValue("Player123!")] 
    string Password
);

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