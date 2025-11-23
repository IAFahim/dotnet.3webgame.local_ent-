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
    private SqliteConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Disable Rate Limiting for Tests
        builder.ConfigureAppConfiguration((context, conf) =>
        {
            conf.AddInMemoryCollection(new Dictionary<string, string?>
            {
                {"RateLimiting:Enabled", "false"}
            });
        });

        builder.ConfigureServices(services =>
        {
            // 1. Remove existing DbContext
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (dbContextDescriptor != null) services.Remove(dbContextDescriptor);

            // 2. Remove existing DbConnection configuration
            var dbConnectionDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbConnection));
            if (dbConnectionDescriptor != null) services.Remove(dbConnectionDescriptor);

            // 3. Create Shared Connection with Busy Timeout
            // Use a unique name to ensure isolation between test classes
            var connectionString = $"DataSource=file:memdb_{Guid.NewGuid()}?mode=memory&cache=shared";
            _connection = new SqliteConnection(connectionString);
            _connection.Open();

            // CRITICAL: Set busy timeout to allow concurrent writes (fixes load test failures)
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = "PRAGMA busy_timeout = 10000;"; // 10 seconds
                command.ExecuteNonQuery();
                command.CommandText = "PRAGMA journal_mode = WAL;"; // Write-Ahead Logging for concurrency
                command.ExecuteNonQuery();
            }

            // 4. Add DbContext with the shared connection
            services.AddDbContext<ApplicationDbContext>((sp, options) =>
            {
                var interceptor = sp.GetRequiredService<AuditableEntityInterceptor>();
                options.UseSqlite(_connection)
                       .AddInterceptors(interceptor);
            });
        });

        builder.UseEnvironment("Testing");
    }

    public HttpClient CreateClientWithDbSetup()
    {
        var client = CreateClient();

        // Ensure database tables are created
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.EnsureCreated();

        return client;
    }

    protected override void Dispose(bool disposing)
    {
        // FIX: Dispose connection BEFORE base.Dispose to avoid NRE
        if (disposing)
        {
            if (_connection != null)
            {
                _connection.Close();
                _connection.Dispose();
                _connection = null;
            }
        }

        base.Dispose(disposing);
    }
}
