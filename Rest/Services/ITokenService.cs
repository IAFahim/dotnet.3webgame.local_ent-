using Rest.Models;

namespace Rest.Services;

/// <summary>
/// Interface for token generation services
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generates a JWT token for the specified user
    /// </summary>
    string GenerateJwtToken(ApplicationUser user);
}
