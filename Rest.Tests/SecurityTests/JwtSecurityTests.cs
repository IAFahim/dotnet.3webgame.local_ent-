using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.IdentityModel.Tokens;
using Rest.Features.Auth;
using Rest.Features.Auth.Register;
using Rest.Tests.Helpers;

namespace Rest.Tests.SecurityTests;

[TestFixture]
public class JwtSecurityTests
{
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new TestWebApplicationFactory<Program>();
        // FIX: Ensure DB is created
        _client = _factory.CreateClientWithDbSetup();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    private TestWebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    [Test]
    public async Task GeneratedToken_ShouldUseHS256Algorithm()
    {
        var token = await RegisterAndGetToken();
        token.Should().NotBeNullOrEmpty();

        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token!);
        jwtToken.Header.Alg.Should().Be(SecurityAlgorithms.HmacSha256);
    }

    [Test]
    public async Task TamperedToken_ShouldBeRejected()
    {
        var validToken = await RegisterAndGetToken();
        var tamperedToken = validToken![..^1] + "X";

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tamperedToken);

        var response = await _client.PostAsync("/api/v1/auth/logout", null);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task ExpiredToken_ShouldBeRejected()
    {
        // Manually create expired token
        var tokenHandler = new JwtSecurityTokenHandler();
        // Use the default test key
        var key = Encoding.UTF8.GetBytes("SuperSecretKeyForTesting1234567890123456");

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }),
            NotBefore = DateTime.UtcNow.AddMinutes(-20),
            Expires = DateTime.UtcNow.AddMinutes(-10),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var expiredToken = tokenHandler.WriteToken(token);

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", expiredToken);

        var response = await _client.PostAsync("/api/v1/auth/logout", null);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Token_ShouldContainRequiredClaims()
    {
        var token = await RegisterAndGetToken();
        token.Should().NotBeNullOrEmpty();

        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token!);

        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub);
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.UniqueName);
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email);
    }

    [Test]
    public async Task TokenWithoutBearer_ShouldBeRejected()
    {
        var token = await RegisterAndGetToken();
        _client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", token!);

        var response = await _client.PostAsync("/api/v1/auth/logout", null);
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

        // This line was throwing because response was 500
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return authResponse!.AccessToken;
    }
}
