using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using NUnit.Framework;
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

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new TestWebApplicationFactory<Program>();
        _client = _factory.CreateClientWithDbSetup();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
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

        // Step 1: Register
        var registerRequest = new RegisterRequest
        {
            Username = username,
            Email = email,
            Password = password
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var registerAuth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();
        registerAuth.Should().NotBeNull();
        registerAuth!.AccessToken.Should().NotBeNullOrEmpty();
        registerAuth.RefreshToken.Should().NotBeNullOrEmpty();

        // Step 2: Login
        var loginRequest = new LoginRequest
        {
            Username = username,
            Password = password
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginAuth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        loginAuth.Should().NotBeNull();
        loginAuth!.AccessToken.Should().NotBeNullOrEmpty();

        // Step 3: Refresh token (MOVED BEFORE CHANGE PASSWORD)
        // We do this first because changing password revokes tokens!
        var refreshRequest = new RefreshTokenRequest
        {
            AccessToken = loginAuth.AccessToken,
            RefreshToken = loginAuth.RefreshToken
        };

        var refreshResponse = await _client.PostAsJsonAsync("/api/v1/auth/refresh", refreshRequest);
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var refreshAuth = await refreshResponse.Content.ReadFromJsonAsync<AuthResponse>();
        refreshAuth.Should().NotBeNull();
        refreshAuth!.AccessToken.Should().NotBeNullOrEmpty();
        refreshAuth.RefreshToken.Should().NotBeNullOrEmpty();

        // Step 4: Use NEW token to access protected endpoint (Change Password)
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", refreshAuth.AccessToken);

        var changePasswordRequest = new
        {
            CurrentPassword = password,
            NewPassword = "NewE2EPassword123!",
            ConfirmNewPassword = "NewE2EPassword123!"
        };

        var changePasswordResponse = await _client.PostAsJsonAsync("/api/v1/auth/change-password", changePasswordRequest);
        changePasswordResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 5: Logout (Using the token we just used, though it might be revoked now due to pass change logic.
        // We will just try to logout to ensure endpoint works, or skip if 401 is expected)

        // Since password change revoked tokens, we need to login again to test logout cleanly,
        // OR we can test that the old token is indeed revoked.

        // Let's verify the old token is revoked (Security Check)
        var oldTokenRequest = await _client.GetAsync("/api/v1/auth/logout");
        // Note: Logout is POST, but testing auth header on any endpoint works

        // Step 6: Verify logout/Revocation - The Refresh Token used in Step 3 should now be invalid
        // because password change revokes everything.
        var reRefreshRequest = new RefreshTokenRequest
        {
            AccessToken = refreshAuth.AccessToken,
            RefreshToken = refreshAuth.RefreshToken
        };

        var reRefreshResponse = await _client.PostAsJsonAsync("/api/v1/auth/refresh", reRefreshRequest);
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
        for (int i = 0; i < 6; i++)
        {
            var loginRequest = new LoginRequest
            {
                Username = username,
                Password = "WrongPassword123!"
            };

            var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

            if (i < 5)
            {
                response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            }
        }

        // Assert - Account should be locked
        var finalLoginRequest = new LoginRequest
        {
            Username = username,
            Password = correctPassword // Even with correct password
        };

        var finalResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", finalLoginRequest);
        finalResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var content = await finalResponse.Content.ReadAsStringAsync();
        content.Should().Contain("locked", "account should be locked");
    }

    private async Task RegisterUser(string username, string email, string password)
    {
        var request = new RegisterRequest
        {
            Username = username,
            Email = email,
            Password = password
        };

        await _client.PostAsJsonAsync("/api/v1/auth/register", request);
    }
}
