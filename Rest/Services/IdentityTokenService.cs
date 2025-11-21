using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Rest.Models;

namespace Rest.Services;

/// <summary>
/// Configuration options for JWT token generation
/// </summary>
public class JwtOptions
{
    public string Key { get; set; } = "SuperSecretKeyForDevelopment12345678901234567890";
    public string Issuer { get; set; } = "GameAuthApi";
    public string Audience { get; set; } = "GameClient";
    public int ExpirationHours { get; set; } = 2;
}

/// <summary>
/// Token service for JWT generation using Identity claims
/// </summary>
public class IdentityTokenService : ITokenService
{
    private readonly JwtOptions _jwtOptions;

    public IdentityTokenService(IOptions<JwtOptions> jwtOptions)
    {
        _jwtOptions = jwtOptions.Value;
    }

    public string GenerateJwtToken(ApplicationUser user)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = BuildClaims(user);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(_jwtOptions.ExpirationHours),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static List<Claim> BuildClaims(ApplicationUser user)
    {
        return new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName!),
            new(ClaimTypes.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("CoinBalance", user.CoinBalance.ToString())
        };
    }
}
