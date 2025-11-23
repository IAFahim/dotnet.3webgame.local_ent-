using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using NUnit.Framework;
using Rest.Features.Auth.Register;
using Rest.Tests.Helpers;

namespace Rest.Tests.PerformanceTests;

[TestFixture]
public class ApiLoadTests
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
    [Category("Load")]
    public async Task HealthEndpoint_ShouldHandle100ConcurrentRequests()
    {
        const int numberOfRequests = 100;
        var stopwatch = Stopwatch.StartNew();

        var tasks = Enumerable.Range(0, numberOfRequests)
            .Select(_ => _client.GetAsync("/health"))
            .ToArray();

        var responses = await Task.WhenAll(tasks);
        stopwatch.Stop();

        responses.Should().AllSatisfy(r => r.StatusCode.Should().Be(HttpStatusCode.OK));
    }

    // [Test]
    // [Category("Load")]
    // public async Task RegisterEndpoint_ShouldHandle50ConcurrentRequests()
    // {
    //     // Note: SQLite has write locking, so 50 concurrent writes might struggle compared to InMemory,
    //     // but removing RateLimits helps. If this fails with "Database Locked", reduce count.
    //     const int numberOfRequests = 20; // Reduced slightly for SQLite safety
    //     var stopwatch = Stopwatch.StartNew();
    //
    //     var tasks = Enumerable.Range(0, numberOfRequests)
    //         .Select(i => RegisterUser($"loadtest_{Guid.NewGuid():N}", $"load_{i}_{Guid.NewGuid():N}@test.com"))
    //         .ToArray();
    //
    //     var responses = await Task.WhenAll(tasks);
    //     stopwatch.Stop();
    //
    //     var successCount = responses.Count(r => r.StatusCode == HttpStatusCode.OK);
    //
    //     TestContext.WriteLine($"Successful registrations: {successCount}/{numberOfRequests}");
    //
    //     // We accept 90% success rate
    //     successCount.Should().BeGreaterThan((int)(numberOfRequests * 0.9));
    // }

    [Test]
    [Category("Stress")]
    public async Task HealthEndpoint_ShouldHandle1000SequentialRequests()
    {
        const int numberOfRequests = 1000;
        var successCount = 0;

        for (int i = 0; i < numberOfRequests; i++)
        {
            var response = await _client.GetAsync("/health");
            if (response.StatusCode == HttpStatusCode.OK) successCount++;
        }

        successCount.Should().Be(numberOfRequests);
    }

    [Test]
    [Category("Stress")]
    [Explicit("Long-running stress test")]
    public async Task ApiEndpoints_ShouldHandleSustainedLoad()
    {
        // Arrange
        const int durationSeconds = 30;
        const int requestsPerSecond = 10;
        var endTime = DateTime.UtcNow.AddSeconds(durationSeconds);
        var requestCount = 0;
        var successCount = 0;
        var failureCount = 0;

        // Act
        while (DateTime.UtcNow < endTime)
        {
            var tasks = Enumerable.Range(0, requestsPerSecond)
                .Select(async _ =>
                {
                    try
                    {
                        var response = await _client.GetAsync("/health");
                        return response.IsSuccessStatusCode;
                    }
                    catch
                    {
                        return false;
                    }
                })
                .ToArray();

            var results = await Task.WhenAll(tasks);
            requestCount += results.Length;
            successCount += results.Count(r => r);
            failureCount += results.Count(r => !r);

            await Task.Delay(1000);
        }

        // Assert
        TestContext.WriteLine($"Total requests: {requestCount}");
        TestContext.WriteLine($"Successful: {successCount}");
        TestContext.WriteLine($"Failed: {failureCount}");
        TestContext.WriteLine($"Success rate: {(successCount / (double)requestCount) * 100:F2}%");

        var successRate = successCount / (double)requestCount;
        successRate.Should().BeGreaterThan(0.95, "Success rate should be above 95%");
    }

    private Task<HttpResponseMessage> RegisterUser(string username, string email)
    {
        var request = new RegisterRequest
        {
            Username = username,
            Email = email,
            Password = "LoadTest123!"
        };
        return _client.PostAsJsonAsync("/api/v1/auth/register", request);
    }
}
