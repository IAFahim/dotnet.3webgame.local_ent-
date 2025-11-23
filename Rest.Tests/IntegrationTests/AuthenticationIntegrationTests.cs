using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Rest.Features.Auth;
using Rest.Features.Auth.Login;
using Rest.Features.Auth.Register;
using Rest.Tests.Helpers;

namespace Rest.Tests.IntegrationTests;

[TestFixture]
public class AuthenticationIntegrationTests
{
    private TestWebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new TestWebApplicationFactory<Program>();
    }

    [SetUp]
    public void SetUp()
    {
        // New client + Reset DB for every test to ensure isolation
        _client = _factory.CreateClientWithDbSetup();
    }

    [TearDown]
    public void TearDown()
    {
        _client?.Dispose();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _factory?.Dispose();
    }

    [Test]
    public async Task Register_WithValidData_ShouldReturnSuccess()
    {
        var request = new RegisterRequest
        {
            Username = $"testuser_{Guid.NewGuid():N}",
            Email = $"test_{Guid.NewGuid():N}@example.com",
            Password = "Password123!"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        authResponse.Should().NotBeNull();
        authResponse!.AccessToken.Should().NotBeNullOrEmpty();
    }

    [Test]
    public async Task Register_WithInvalidEmail_ShouldReturnBadRequest()
    {
        var request = new RegisterRequest { Username = "testuser", Email = "invalid-email", Password = "Password123!" };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadAsStringAsync();
        // FIX: FastEndpoints/System.Text.Json uses camelCase for property names ("email", not "Email")
        content.Should().Contain("email");
    }

    [Test]
    public async Task Register_WithWeakPassword_ShouldReturnBadRequest()
    {
        var request = new RegisterRequest
        {
            Username = $"testuser_{Guid.NewGuid():N}",
            Email = $"test_{Guid.NewGuid():N}@example.com",
            Password = "weak"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadAsStringAsync();
        // FIX: Check for the specific error logic or simply the property name in camelCase
        content.Should().Contain("password");
    }

    [Test]
    public async Task Login_WithValidCredentials_ShouldReturnSuccess()
    {
        // 1. Arrange - Create User
        var username = $"testuser_{Guid.NewGuid():N}";
        var email = $"test_{Guid.NewGuid():N}@example.com";
        var password = "Password123!";
        await RegisterUser(username, email, password);

        // 2. Act - Login
        var loginRequest = new LoginRequest { Username = username, Password = password };
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // 3. Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        authResponse.Should().NotBeNull();
        authResponse!.AccessToken.Should().NotBeNullOrEmpty();
    }

    [Test]
    public async Task Login_WithInvalidCredentials_ShouldReturnUnauthorized()
    {
        var loginRequest = new LoginRequest { Username = "nonexistent", Password = "WrongPassword123!" };
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task ChangePassword_WithValidToken_ShouldReturnSuccess()
    {
        var username = $"testuser_{Guid.NewGuid():N}";
        var email = $"test_{Guid.NewGuid():N}@example.com";
        var oldPassword = "OldPassword123!";
        var newPassword = "NewPassword123!";

        var token = await RegisterAndGetToken(username, email, oldPassword);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var changePasswordRequest = new
        {
            CurrentPassword = oldPassword,
            NewPassword = newPassword,
            ConfirmNewPassword = newPassword
        };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/change-password", changePasswordRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task ChangePassword_WithoutToken_ShouldReturnUnauthorized()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var changePasswordRequest = new
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword123!",
            ConfirmNewPassword = "NewPassword123!"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/change-password", changePasswordRequest);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // --- Helpers ---

    private async Task RegisterUser(string username, string email, string password)
    {
        var request = new RegisterRequest { Username = username, Email = email, Password = password };
        await _client.PostAsJsonAsync("/api/v1/auth/register", request);
    }

    private async Task<string> RegisterAndGetToken(string username, string email, string password)
    {
        var request = new RegisterRequest { Username = username, Email = email, Password = password };
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return authResponse!.AccessToken;
    }
}
