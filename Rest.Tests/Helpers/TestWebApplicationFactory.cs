using System.Data.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rest.Data;
using Rest.Data.Interceptors;

namespace Rest.Tests.Helpers;

public class TestWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    private DbConnection _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // 1. Disable Rate Limiting for tests to prevent 429 errors
        builder.ConfigureAppConfiguration((context, conf) =>
        {
            conf.AddInMemoryCollection(new Dictionary<string, string?>
            {
                {"RateLimiting:Enabled", "false"}
            });
        });

        builder.ConfigureServices(services =>
        {
            // 2. Remove the app's existing DbContext registration
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (dbContextDescriptor != null) services.Remove(dbContextDescriptor);

            var dbConnectionDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbConnection));
            if (dbConnectionDescriptor != null) services.Remove(dbConnectionDescriptor);

            // 3. Create a single shared SQLite connection for this factory
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            services.AddSingleton<DbConnection>(_connection);

            // 4. Register DbContext using the shared connection AND the Interceptor
            services.AddDbContext<ApplicationDbContext>((sp, options) =>
            {
                var interceptor = sp.GetRequiredService<AuditableEntityInterceptor>();
                options.UseSqlite(_connection)
                       .AddInterceptors(interceptor);
            });
        });

        builder.UseEnvironment("Testing");
    }

    // Helper to ensure DB is created before tests run
    public HttpClient CreateClientWithDbSetup()
    {
        var client = CreateClient();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.EnsureCreated();

        return client;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _connection?.Dispose();
    }
}
