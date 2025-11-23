using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Rest.Features.Auth;
using Rest.Features.Auth.Login;
using Rest.Features.Auth.RefreshToken;
using Rest.Features.Auth.Register;
using Rest.Tests.Helpers;

namespace Rest.Tests.E2ETests;

[TestFixture]
public class CompleteAuthFlowTests
{
    private TestWebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        // Re-create the factory and client for EVERY test.
        // This is slightly slower but guarantees 100% isolation and fixes the "14 tests fail" issue.
        _factory = new TestWebApplicationFactory<Program>();
        _client = _factory.CreateClientWithDbSetup();
    }

    [TearDown]
    public void TearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Test]
    public async Task CompleteUserJourney_RegisterLoginRefreshLogout_ShouldWorkEndToEnd()
    {
        var username = $"e2euser_{Guid.NewGuid():N}";
        var email = $"e2e_{Guid.NewGuid():N}@example.com";
        var password = "E2EPassword123!";

        // 1. Register
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterRequest { Username = username, Email = email, Password = password });
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

        // 2. Login
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest { Username = username, Password = password });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginAuth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();

        // 3. Refresh Token
        var refreshResponse = await _client.PostAsJsonAsync("/api/v1/auth/refresh",
            new RefreshTokenRequest { AccessToken = loginAuth!.AccessToken, RefreshToken = loginAuth.RefreshToken });
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshAuth = await refreshResponse.Content.ReadFromJsonAsync<AuthResponse>();

        // 4. Change Password (using the NEW token from step 3)
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", refreshAuth!.AccessToken);
        var changePwResponse = await _client.PostAsJsonAsync("/api/v1/auth/change-password", new
        {
            CurrentPassword = password,
            NewPassword = "NewE2EPassword123!",
            ConfirmNewPassword = "NewE2EPassword123!"
        });
        changePwResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 5. Verify Old Refresh Token is now Invalid (Because password change revokes sessions)
        var reRefreshResponse = await _client.PostAsJsonAsync("/api/v1/auth/refresh",
            new RefreshTokenRequest { AccessToken = refreshAuth.AccessToken, RefreshToken = refreshAuth.RefreshToken });

        // This is the critical check: Password change = Revocation
        reRefreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task LoginWithWrongPassword_MultipleTimes_ShouldLockAccount()
    {
        // Arrange
        var username = $"locktest_{Guid.NewGuid():N}";
        var email = $"lock_{Guid.NewGuid():N}@example.com";
        var correctPassword = "CorrectPassword123!";

        // Register user
        await RegisterUser(username, email, correctPassword);

        // Act - Try to login with wrong password 6 times
        for (var i = 0; i < 6; i++)
        {
            var loginRequest = new LoginRequest { Username = username, Password = "WrongPassword123!" };

            var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

            if (i < 5)
            {
                response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            }
        }

        // Assert - Account should be locked
        var finalLoginRequest = new LoginRequest
        {
            Username = username, Password = correctPassword // Even with correct password
        };

        var finalResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", finalLoginRequest);
        finalResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var content = await finalResponse.Content.ReadAsStringAsync();
        content.Should().Contain("locked", "account should be locked");
    }

    private async Task RegisterUser(string username, string email, string password)
    {
        var request = new RegisterRequest { Username = username, Email = email, Password = password };

        await _client.PostAsJsonAsync("/api/v1/auth/register", request);
    }
}
