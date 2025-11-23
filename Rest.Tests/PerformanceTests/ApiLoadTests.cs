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
        _client = _factory.CreateClient();
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
        // Arrange
        const int numberOfRequests = 100;
        var stopwatch = Stopwatch.StartNew();

        // Act
        var tasks = Enumerable.Range(0, numberOfRequests)
            .Select(_ => _client.GetAsync("/health"))
            .ToArray();

        var responses = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        responses.Should().AllSatisfy(r => r.StatusCode.Should().Be(HttpStatusCode.OK));
        
        var averageTime = stopwatch.ElapsedMilliseconds / (double)numberOfRequests;
        TestContext.WriteLine($"Average response time: {averageTime}ms");
        TestContext.WriteLine($"Total time: {stopwatch.ElapsedMilliseconds}ms");
        
        averageTime.Should().BeLessThan(100, "Average response time should be under 100ms");
    }

    [Test]
    [Category("Load")]
    public async Task RegisterEndpoint_ShouldHandle50ConcurrentRequests()
    {
        // Arrange
        const int numberOfRequests = 50;
        var stopwatch = Stopwatch.StartNew();

        // Act
        var tasks = Enumerable.Range(0, numberOfRequests)
            .Select(i => RegisterUser($"loadtest_{Guid.NewGuid():N}", $"load_{i}@test.com"))
            .ToArray();

        var responses = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        var successCount = responses.Count(r => r.StatusCode == HttpStatusCode.OK);
        
        TestContext.WriteLine($"Successful registrations: {successCount}/{numberOfRequests}");
        TestContext.WriteLine($"Total time: {stopwatch.ElapsedMilliseconds}ms");
        TestContext.WriteLine($"Average time per request: {stopwatch.ElapsedMilliseconds / (double)numberOfRequests}ms");
        
        successCount.Should().BeGreaterThan((int)(numberOfRequests * 0.9), 
            "At least 90% of requests should succeed");
    }

    [Test]
    [Category("Stress")]
    public async Task HealthEndpoint_ShouldHandle1000SequentialRequests()
    {
        // Arrange
        const int numberOfRequests = 1000;
        var successCount = 0;
        var stopwatch = Stopwatch.StartNew();

        // Act
        for (int i = 0; i < numberOfRequests; i++)
        {
            var response = await _client.GetAsync("/health");
            if (response.StatusCode == HttpStatusCode.OK)
            {
                successCount++;
            }
        }

        stopwatch.Stop();

        // Assert
        TestContext.WriteLine($"Successful requests: {successCount}/{numberOfRequests}");
        TestContext.WriteLine($"Total time: {stopwatch.ElapsedMilliseconds}ms");
        TestContext.WriteLine($"Requests per second: {numberOfRequests / (stopwatch.ElapsedMilliseconds / 1000.0):F2}");
        
        successCount.Should().Be(numberOfRequests, "All requests should succeed");
        
        var averageTime = stopwatch.ElapsedMilliseconds / (double)numberOfRequests;
        averageTime.Should().BeLessThan(50, "Average response time should be under 50ms");
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
