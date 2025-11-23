using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Rest.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static string GetUserId(this ClaimsPrincipal principal)
    {
        // 1. Try "sub" (Standard JWT, used if Mapping is Cleared)
        var claim = principal.FindFirst(JwtRegisteredClaimNames.Sub);
        if (claim != null)
        {
            return claim.Value;
        }

        // 2. Try "ClaimTypes.NameIdentifier" (ASP.NET Default)
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

        throw new UnauthorizedAccessException("Invalid Token: User ID claim (sub) is missing.");
    }
}
