using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.IdentityModel.Tokens;
using Rest.Features.Auth.Register;
using Rest.Tests.Helpers;

namespace Rest.Tests.SecurityTests;

[TestFixture]
public class JwtSecurityTests
{
    private TestWebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new TestWebApplicationFactory<Program>();
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Test]
    public async Task GeneratedToken_ShouldUseHS256Algorithm()
    {
        // Arrange & Act
        var token = await RegisterAndGetToken();

        // Assert
        token.Should().NotBeNullOrEmpty("registration should return a valid token");

        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token!);

        jwtToken.Header.Alg.Should().Be(SecurityAlgorithms.HmacSha256);
    }

    [Test]
    public async Task TamperedToken_ShouldBeRejected()
    {
        // Arrange
        var validToken = await RegisterAndGetToken();

        validToken.Should().NotBeNullOrEmpty("registration should return a valid token");

        // Tamper with token by changing one character
        var tamperedToken = validToken![..^1] + "X";

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tamperedToken);

        // Act
        var response = await _client.PostAsync("/api/v1/auth/logout", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task ExpiredToken_ShouldBeRejected()
    {
        // Arrange - Create a token that's already expired
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes("SuperSecretKeyForTesting1234567890123456");

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }),
            NotBefore = DateTime.UtcNow.AddMinutes(-20),
            Expires = DateTime.UtcNow.AddMinutes(-10), // Already expired
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var expiredToken = tokenHandler.WriteToken(token);

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", expiredToken);

        // Act
        var response = await _client.PostAsync("/api/v1/auth/logout", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Token_ShouldContainRequiredClaims()
    {
        // Arrange & Act
        var token = await RegisterAndGetToken();

        // Assert
        token.Should().NotBeNullOrEmpty("registration should return a valid token");

        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token!);

        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub);
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.UniqueName);
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email);
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Jti);
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Iat);
    }

    [Test]
    public async Task TokenWithoutBearer_ShouldBeRejected()
    {
        // Arrange
        var token = await RegisterAndGetToken();

        token.Should().NotBeNullOrEmpty("registration should return a valid token");

        // Don't use "Bearer" prefix (using TryAddWithoutValidation to avoid format exceptions)
        _client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", token!);

        // Act
        var response = await _client.PostAsync("/api/v1/auth/logout", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<string> RegisterAndGetToken()
    {
        var request = new RegisterRequest
        {
            Username = $"sectest_{Guid.NewGuid():N}",
            Email = $"sectest_{Guid.NewGuid():N}@example.com",
            Password = "SecurePassword123!"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);
        var authResponse = await response.Content.ReadFromJsonAsync<Rest.Features.Auth.AuthResponse>();
        return authResponse!.AccessToken;
    }
}
