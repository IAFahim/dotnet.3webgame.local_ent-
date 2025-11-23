using System.ComponentModel.DataAnnotations;

namespace Rest.Features.Auth.RefreshToken;

public sealed class RefreshTokenRequest
{
    [Required]
    public required string AccessToken { get; init; }
    
    [Required]
    public required string RefreshToken { get; init; }
}

