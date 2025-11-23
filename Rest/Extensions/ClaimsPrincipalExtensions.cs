using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Rest.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static string GetUserId(this ClaimsPrincipal principal)
    {
        if (principal == null)
        {
            throw new ArgumentNullException(nameof(principal));
        }

        // 1. Try "sub" (Standard JWT)
        var claim = principal.FindFirst(JwtRegisteredClaimNames.Sub);
        if (claim != null)
        {
            return claim.Value;
        }

        // 2. Try "ClaimTypes.NameIdentifier" (ASP.NET Default mapping)
        claim = principal.FindFirst(ClaimTypes.NameIdentifier);
        if (claim != null)
        {
            return claim.Value;
        }

        // 3. Try "id" (Custom fallback)
        claim = principal.FindFirst("id");
        if (claim != null)
        {
            return claim.Value;
        }

        // 4. Try long schema version
        claim = principal.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
        if (claim != null)
        {
            return claim.Value;
        }

        throw new UnauthorizedAccessException("Invalid Token: User ID claim is missing.");
    }
}
