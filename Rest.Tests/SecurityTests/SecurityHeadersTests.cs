using FluentAssertions;
using NUnit.Framework;
using Rest.Tests.Helpers;

namespace Rest.Tests.SecurityTests;

[TestFixture]
public class SecurityHeadersTests
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
    public async Task Response_ShouldIncludeSecurityHeaders()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.Headers.Should().ContainKey("X-Content-Type-Options");
        response.Headers.GetValues("X-Content-Type-Options").Should().Contain("nosniff");

        response.Headers.Should().ContainKey("X-Frame-Options");
        response.Headers.GetValues("X-Frame-Options").Should().Contain("DENY");

        response.Headers.Should().ContainKey("X-XSS-Protection");
        response.Headers.GetValues("X-XSS-Protection").Should().Contain("1; mode=block");

        response.Headers.Should().ContainKey("Referrer-Policy");
        response.Headers.GetValues("Referrer-Policy").Should().Contain("strict-origin-when-cross-origin");
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
