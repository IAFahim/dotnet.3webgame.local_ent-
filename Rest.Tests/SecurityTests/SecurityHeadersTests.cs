using FluentAssertions;
using Rest.Tests.Helpers;

namespace Rest.Tests.SecurityTests;

[TestFixture]
public class SecurityHeadersTests
{
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

    private TestWebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    [Test]
    public async Task Response_ShouldIncludeSecurityHeaders()
    {
        var response = await _client.GetAsync("/health");

        response.Headers.Should().ContainKey("X-Content-Type-Options");
        response.Headers.Should().ContainKey("X-Frame-Options");
        response.Headers.Should().ContainKey("X-XSS-Protection");
    }

    [Test]
    public async Task Response_ShouldNotIncludeServerHeader()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.Headers.Should().NotContainKey("Server");
        response.Headers.Should().NotContainKey("X-Powered-By");
    }

    [Test]
    public async Task Response_ShouldIncludePermissionsPolicyHeader()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.Headers.Should().ContainKey("Permissions-Policy");
        var policy = response.Headers.GetValues("Permissions-Policy").First();
        policy.Should().Contain("camera=()");
        policy.Should().Contain("microphone=()");
    }
}
