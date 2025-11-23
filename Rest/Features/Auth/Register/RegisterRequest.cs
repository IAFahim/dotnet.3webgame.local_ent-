using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Rest.Features.Auth.Register;

public sealed class RegisterRequest
{
    [Required]
    [StringLength(50, MinimumLength = 3)]
    [DefaultValue("new_hero")]
    public required string Username { get; init; }
    
    [Required]
    [EmailAddress]
    [StringLength(100)]
    [DefaultValue("hero@game.com")]
    public required string Email { get; init; }
    
    [Required]
    [StringLength(100, MinimumLength = 8)]
    [DefaultValue("Password123!")]
    public required string Password { get; init; }
}

