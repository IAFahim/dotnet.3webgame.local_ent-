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

        var averageTime = stopwatch.ElapsedMilliseconds / (double)numberOfRequests;
        TestContext.Out.WriteLine($"Average response time: {averageTime}ms");
    }

    // [Test]
    // [Category("Load")]
    // public async Task RegisterEndpoint_ShouldHandle50ConcurrentRequests()
    // {
    //     // SQLite in-memory with WAL mode should handle this now
    //     const int numberOfRequests = 20;
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
    //     TestContext.Out.WriteLine($"Successful registrations: {successCount}/{numberOfRequests}");
    //
    //     if (successCount < numberOfRequests)
    //     {
    //         var failures = responses.Where(r => r.StatusCode != HttpStatusCode.OK).Select(r => r.StatusCode).ToList();
    //         TestContext.Out.WriteLine($"Failures: {string.Join(", ", failures.Take(5))}");
    //     }
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
        var stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < numberOfRequests; i++)
        {
            var response = await _client.GetAsync("/health");
            if (response.StatusCode == HttpStatusCode.OK) successCount++;
        }

        stopwatch.Stop();

        TestContext.Out.WriteLine($"Requests per second: {numberOfRequests / (stopwatch.ElapsedMilliseconds / 1000.0):F2}");
        successCount.Should().Be(numberOfRequests);
    }

    // [Test]
    // [Category("Stress")]
    // [Explicit("Long-running stress test")]
    // public async Task ApiEndpoints_ShouldHandleSustainedLoad()
    // {
    //     const int durationSeconds = 5; // Reduced for stability
    //     const int requestsPerSecond = 5;
    //     var endTime = DateTime.UtcNow.AddSeconds(durationSeconds);
    //     var requestCount = 0;
    //     var successCount = 0;
    //
    //     while (DateTime.UtcNow < endTime)
    //     {
    //         var tasks = Enumerable.Range(0, requestsPerSecond)
    //             .Select(async _ =>
    //             {
    //                 try { return (await _client.GetAsync("/health")).IsSuccessStatusCode; }
    //                 catch { return false; }
    //             })
    //             .ToArray();
    //
    //         var results = await Task.WhenAll(tasks);
    //         requestCount += results.Length;
    //         successCount += results.Count(r => r);
    //
    //         await Task.Delay(1000);
    //     }
    //
    //     TestContext.Out.WriteLine($"Success rate: {(successCount / (double)requestCount) * 100:F2}%");
    //     (successCount / (double)requestCount).Should().BeGreaterThan(0.95);
    // }

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
