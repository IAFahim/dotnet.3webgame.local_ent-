using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Rest.Features.Auth.ChangePassword;

public sealed class ChangePasswordRequest
{
    [Required]
    [StringLength(100, MinimumLength = 8)]
    [DefaultValue("Password123!")]
    public required string CurrentPassword { get; init; }
    
    [Required]
    [StringLength(100, MinimumLength = 8)]
    [DefaultValue("NewPassword123!")]
    public required string NewPassword { get; init; }
    
    [Required]
    [Compare(nameof(NewPassword))]
    [DefaultValue("NewPassword123!")]
    public required string ConfirmNewPassword { get; init; }
}

