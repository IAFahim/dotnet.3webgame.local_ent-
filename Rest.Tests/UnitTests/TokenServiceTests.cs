using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Rest.Models;
using Rest.Options;
using Rest.Services;

namespace Rest.Tests.UnitTests;

[TestFixture]
public class TokenServiceTests
{
    private TokenService _tokenService = null!;
    private JwtSettings _jwtSettings = null!;
    private TimeProvider _timeProvider = null!;

    [SetUp]
    public void Setup()
    {
        _jwtSettings = new JwtSettings
        {
            Key = "SuperSecretKeyForTesting1234567890123456",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpirationHours = 1
        };

        var options = Microsoft.Extensions.Options.Options.Create(_jwtSettings);
        _timeProvider = TimeProvider.System;
        _tokenService = new TokenService(options, _timeProvider);
    }

    [Test]
    public void GenerateJwtToken_ShouldReturnValidToken()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "testuser",
            Email = "test@example.com"
        };

        // Act
        var token = _tokenService.GenerateJwtToken(user);

        // Assert
        token.Should().NotBeNullOrEmpty();

        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);

        jwtToken.Issuer.Should().Be(_jwtSettings.Issuer);
        jwtToken.Audiences.Should().Contain(_jwtSettings.Audience);
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == user.Id);
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.UniqueName && c.Value == user.UserName);
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == user.Email);
    }

    [Test]
    public void GenerateJwtToken_ShouldExpireAfterConfiguredHours()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "testuser",
            Email = "test@example.com"
        };

        var beforeGeneration = _timeProvider.GetUtcNow().UtcDateTime;

        // Act
        var token = _tokenService.GenerateJwtToken(user);

        // Assert
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);

        // Get expiration from exp claim (Unix timestamp)
        var expClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "exp")?.Value;
        expClaim.Should().NotBeNullOrEmpty();

        var expUnixTime = long.Parse(expClaim!);
        var expirationTime = DateTimeOffset.FromUnixTimeSeconds(expUnixTime);

        var expectedExpiration = beforeGeneration.AddHours(_jwtSettings.ExpirationHours);

        // Token should expire approximately at the expected time (within 10 seconds tolerance)
        var timeDifference = (expirationTime - expectedExpiration).TotalSeconds;
        timeDifference.Should().BeInRange(-10, 10,
            "token expiration should be within 10 seconds of the expected time");

        // Token should not be expired yet
        expirationTime.Should().BeAfter(_timeProvider.GetUtcNow().UtcDateTime);
    }

    [Test]
    public void GenerateRefreshToken_ShouldReturnValidToken()
    {
        // Act
        var refreshToken = _tokenService.GenerateRefreshToken();

        // Assert
        refreshToken.Should().NotBeNull();
        refreshToken.Token.Should().NotBeNullOrEmpty();
        refreshToken.Token.Length.Should().BeGreaterThan(50); // Base64 of 64 bytes
        refreshToken.Created.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        refreshToken.Expires.Should().BeCloseTo(DateTime.UtcNow.AddDays(30), TimeSpan.FromSeconds(5));
        refreshToken.Revoked.Should().BeNull();
    }

    [Test]
    public void GenerateRefreshToken_ShouldGenerateUniqueTokens()
    {
        // Act
        var token1 = _tokenService.GenerateRefreshToken();
        var token2 = _tokenService.GenerateRefreshToken();

        // Assert
        token1.Token.Should().NotBe(token2.Token);
    }

    [Test]
    public void RefreshToken_IsActive_ShouldReturnTrueForValidToken()
    {
        // Arrange
        var refreshToken = new RefreshToken
        {
            Token = "valid-token",
            Expires = DateTime.UtcNow.AddDays(1),
            Created = DateTime.UtcNow,
            Revoked = null
        };

        // Assert
        refreshToken.IsActive.Should().BeTrue();
    }

    [Test]
    public void RefreshToken_IsActive_ShouldReturnFalseForExpiredToken()
    {
        // Arrange
        var refreshToken = new RefreshToken
        {
            Token = "expired-token",
            Expires = DateTime.UtcNow.AddDays(-1),
            Created = DateTime.UtcNow.AddDays(-31),
            Revoked = null
        };

        // Assert
        refreshToken.IsActive.Should().BeFalse();
    }

    [Test]
    public void RefreshToken_IsActive_ShouldReturnFalseForRevokedToken()
    {
        // Arrange
        var refreshToken = new RefreshToken
        {
            Token = "revoked-token",
            Expires = DateTime.UtcNow.AddDays(1),
            Created = DateTime.UtcNow,
            Revoked = DateTime.UtcNow
        };

        // Assert
        refreshToken.IsActive.Should().BeFalse();
    }
}
