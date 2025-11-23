using System.Collections.Concurrent;
using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Rest.Models;
using Rest.Options;
using Rest.Services;

namespace Rest.Tests.PerformanceTests;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class TokenGenerationBenchmarks
{
    private ApplicationUser _testUser = null!;
    private TokenService _tokenService = null!;

    [GlobalSetup]
    public void Setup()
    {
        var jwtSettings = new JwtSettings
        {
            Key = "SuperSecretKeyForTesting1234567890123456",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpirationHours = 1
        };

        var options = Microsoft.Extensions.Options.Options.Create(jwtSettings);
        _tokenService = new TokenService(options, TimeProvider.System);

        _testUser = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(), UserName = "benchmarkuser", Email = "benchmark@example.com"
        };
    }

    [Benchmark]
    public string GenerateJwtToken() => _tokenService.GenerateJwtToken(_testUser);

    [Benchmark]
    public RefreshToken GenerateRefreshToken() => _tokenService.GenerateRefreshToken();

    [Benchmark]
    public void GenerateBothTokens()
    {
        var jwtToken = _tokenService.GenerateJwtToken(_testUser);
        var refreshToken = _tokenService.GenerateRefreshToken();
    }
}

[TestFixture]
public class TokenGenerationPerformanceTests
{
    [Test]
    [Explicit("Performance test - run manually")]
    public void RunBenchmarks()
    {
        var summary = BenchmarkRunner.Run<TokenGenerationBenchmarks>();
        Assert.Pass("Benchmarks completed. Check output for results.");
    }

    [Test]
    public void TokenGeneration_ShouldCompleteUnder100Milliseconds()
    {
        // Arrange
        var jwtSettings = new JwtSettings
        {
            Key = "SuperSecretKeyForTesting1234567890123456",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpirationHours = 1
        };

        var options = Microsoft.Extensions.Options.Options.Create(jwtSettings);
        var tokenService = new TokenService(options, TimeProvider.System);

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(), UserName = "perftest", Email = "perf@example.com"
        };

        var stopwatch = Stopwatch.StartNew();

        // Act
        for (var i = 0; i < 100; i++)
        {
            tokenService.GenerateJwtToken(user);
        }

        stopwatch.Stop();

        // Assert
        var averageTime = stopwatch.ElapsedMilliseconds / 100.0;
        Assert.That(averageTime, Is.LessThan(10),
            $"Average token generation time was {averageTime}ms, expected less than 10ms");
    }

    [Test]
    public void ConcurrentTokenGeneration_ShouldBeThreadSafe()
    {
        // Arrange
        var jwtSettings = new JwtSettings
        {
            Key = "SuperSecretKeyForTesting1234567890123456",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpirationHours = 1
        };

        var options = Microsoft.Extensions.Options.Options.Create(jwtSettings);
        var tokenService = new TokenService(options, TimeProvider.System);

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(), UserName = "concurrenttest", Email = "concurrent@example.com"
        };

        var tokens = new ConcurrentBag<string>();

        // Act
        Parallel.For(0, 100, _ =>
        {
            var token = tokenService.GenerateJwtToken(user);
            tokens.Add(token);
        });

        // Assert
        Assert.That(tokens.Count, Is.EqualTo(100));
        Assert.That(tokens.Distinct().Count(), Is.EqualTo(100),
            "All tokens should be unique");
    }
}
