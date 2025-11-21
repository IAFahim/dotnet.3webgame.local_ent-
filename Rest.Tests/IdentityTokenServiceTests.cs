using Microsoft.Extensions.Options;
using Rest.Models;
using Rest.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Xunit;

namespace Rest.Tests;

public class IdentityTokenServiceTests
{
    private readonly IdentityTokenService _tokenService;
    private readonly JwtOptions _jwtOptions;

    public IdentityTokenServiceTests()
    {
        _jwtOptions = new JwtOptions
        {
            Key = "SuperSecretKeyForTesting1234567890123456",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpirationHours = 2
        };

        var options = Options.Create(_jwtOptions);
        _tokenService = new IdentityTokenService(options);
    }

    [Fact]
    public void GenerateJwtToken_ValidUser_ReturnsToken()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "123",
            UserName = "testuser",
            Email = "test@test.com",
            CoinBalance = 100.00m
        };

        // Act
        var token = _tokenService.GenerateJwtToken(user);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
    }

    [Fact]
    public void GenerateJwtToken_ValidUser_ContainsCorrectClaims()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "test-id-123",
            UserName = "testuser",
            Email = "test@test.com",
            CoinBalance = 250.50m
        };

        // Act
        var token = _tokenService.GenerateJwtToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Assert
        Assert.Equal("test-id-123", jwtToken.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
        Assert.Equal("testuser", jwtToken.Claims.First(c => c.Type == ClaimTypes.Name).Value);
        Assert.Equal("test@test.com", jwtToken.Claims.First(c => c.Type == ClaimTypes.Email).Value);
        Assert.Equal("250.50", jwtToken.Claims.First(c => c.Type == "CoinBalance").Value);
    }

    [Fact]
    public void GenerateJwtToken_ValidUser_HasCorrectIssuerAndAudience()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "123",
            UserName = "testuser",
            Email = "test@test.com",
            CoinBalance = 100.00m
        };

        // Act
        var token = _tokenService.GenerateJwtToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Assert
        Assert.Equal(_jwtOptions.Issuer, jwtToken.Issuer);
        Assert.Contains(_jwtOptions.Audience, jwtToken.Audiences);
    }

    [Fact]
    public void GenerateJwtToken_ValidUser_HasValidExpiration()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "123",
            UserName = "testuser",
            Email = "test@test.com",
            CoinBalance = 100.00m
        };

        var beforeGeneration = DateTime.UtcNow;

        // Act
        var token = _tokenService.GenerateJwtToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        var afterGeneration = DateTime.UtcNow;

        // Assert - Allow some tolerance for test execution time
        var minExpiration = beforeGeneration.AddHours(_jwtOptions.ExpirationHours).AddSeconds(-5);
        var maxExpiration = afterGeneration.AddHours(_jwtOptions.ExpirationHours).AddSeconds(5);
        Assert.True(jwtToken.ValidTo >= minExpiration && jwtToken.ValidTo <= maxExpiration,
            $"Token expiration {jwtToken.ValidTo} is outside expected range [{minExpiration}, {maxExpiration}]");
    }

    [Fact]
    public void GenerateJwtToken_MultipleCalls_GeneratesUniqueTokesWithUniqueJti()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "123",
            UserName = "testuser",
            Email = "test@test.com",
            CoinBalance = 100.00m
        };

        // Act
        var token1 = _tokenService.GenerateJwtToken(user);
        var token2 = _tokenService.GenerateJwtToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jwtToken1 = handler.ReadJwtToken(token1);
        var jwtToken2 = handler.ReadJwtToken(token2);

        var jti1 = jwtToken1.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
        var jti2 = jwtToken2.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;

        // Assert
        Assert.NotEqual(token1, token2);
        Assert.NotEqual(jti1, jti2);
    }
}
