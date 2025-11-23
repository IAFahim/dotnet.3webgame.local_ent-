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
        builder.ConfigureAppConfiguration((_, conf) =>
        {
            // Force disable rate limiting so tests don't get 429s randomly
            conf.AddInMemoryCollection(new Dictionary<string, string?> { { "RateLimiting:Enabled", "false" } });
        });

        builder.ConfigureServices(services =>
        {
            // 1. Remove existing DbContext definition
            var dbContextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (dbContextDescriptor != null) services.Remove(dbContextDescriptor);

            var dbConnectionDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbConnection));
            if (dbConnectionDescriptor != null) services.Remove(dbConnectionDescriptor);

            // 2. Create a specific In-Memory connection for THIS factory instance
            // We use a random GUID to ensure if NUnit runs in parallel, DBs don't collide
            var connectionString = $"DataSource=file:memdb_{Guid.NewGuid()}?mode=memory&cache=shared";
            _connection = new SqliteConnection(connectionString);
            _connection.Open();

            // 3. Register the connection and Context
            services.AddDbContext<ApplicationDbContext>((sp, options) =>
            {
                var interceptor = sp.GetRequiredService<AuditableEntityInterceptor>();
                options.UseSqlite(_connection).AddInterceptors(interceptor);
            });
        });

        builder.UseEnvironment("Testing");
    }

    public HttpClient CreateClientWithDbSetup()
    {
        var client = CreateClient();

        // Ensure database is created and seeded for THIS connection
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();

        return client;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _connection?.Close();
            _connection?.Dispose();
        }
        base.Dispose(disposing);
    }
}
