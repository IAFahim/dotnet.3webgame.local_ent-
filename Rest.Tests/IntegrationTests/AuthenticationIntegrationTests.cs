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
    [OneTimeSetUp]
    public void OneTimeSetUp() => _factory = new TestWebApplicationFactory<Program>();

    [SetUp]
    public void SetUp()
    {
        // Recreate client and DB for every test to ensure isolation
        _client = _factory.CreateClientWithDbSetup();
    }

    [TearDown]
    public void TearDown() => _client?.Dispose();

    [OneTimeTearDown]
    public void OneTimeTearDown() => _factory?.Dispose();

    private TestWebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

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

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        authResponse.Should().NotBeNull();
    }

    [Test]
    public async Task Register_WithInvalidEmail_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new RegisterRequest { Username = "testuser", Email = "invalid-email", Password = "Password123!" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Register_WithWeakPassword_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = $"testuser_{Guid.NewGuid():N}",
            Email = $"test_{Guid.NewGuid():N}@example.com",
            Password = "weak"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Login_WithValidCredentials_ShouldReturnSuccess()
    {
        // Arrange
        var username = $"testuser_{Guid.NewGuid():N}";
        var email = $"test_{Guid.NewGuid():N}@example.com";
        var password = "Password123!";

        await RegisterUser(username, email, password);

        var loginRequest = new LoginRequest { Username = username, Password = password };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        authResponse.Should().NotBeNull();
        authResponse!.AccessToken.Should().NotBeNullOrEmpty();
        authResponse.RefreshToken.Should().NotBeNullOrEmpty();
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
        // Arrange
        var username = $"testuser_{Guid.NewGuid():N}";
        var email = $"test_{Guid.NewGuid():N}@example.com";
        var oldPassword = "OldPassword123!";
        var newPassword = "NewPassword123!";

        var token = await RegisterAndGetToken(username, email, oldPassword);

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var changePasswordRequest = new
        {
            CurrentPassword = oldPassword, NewPassword = newPassword, ConfirmNewPassword = newPassword
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/change-password", changePasswordRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task ChangePassword_WithoutToken_ShouldReturnUnauthorized()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = null;

        var changePasswordRequest = new
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword123!",
            ConfirmNewPassword = "NewPassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/change-password", changePasswordRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }


    public void Dispose()
    {
        // Already disposed in OneTimeTearDown
    }

    // Helper needed for other tests
    private async Task RegisterUser(string username, string email, string password)
    {
        var request = new RegisterRequest { Username = username, Email = email, Password = password };
        await _client.PostAsJsonAsync("/api/v1/auth/register", request);
    }

    // Helper needed for other tests
    private async Task<string> RegisterAndGetToken(string username, string email, string password)
    {
        var request = new RegisterRequest { Username = username, Email = email, Password = password };
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return authResponse!.AccessToken;
    }
}
