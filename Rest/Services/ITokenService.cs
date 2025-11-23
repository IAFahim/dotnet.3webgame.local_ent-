using System.Security.Claims;
using Rest.Models;

namespace Rest.Services;

public interface ITokenService
{
    string GenerateJwtToken(ApplicationUser user);
    RefreshToken GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string? token);
}
