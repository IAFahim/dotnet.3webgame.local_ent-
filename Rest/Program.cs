using System.IdentityModel.Tokens.Jwt;
using DotNetEnv;
using Rest.Extensions;
using Serilog;

// 1. Bootstrap Setup
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("🚀 Starting Game Auth API...");

    Env.TraversePath().Load();
    var builder = WebApplication.CreateBuilder(args);
    builder.Configuration.AddEnvironmentVariables();

    // 2. Configure Host (Logging)
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
    );

    // 3. Register Services (The "What")
    //    - Infrastructure: Database, Identity, JWT (Your existing extension)
    //    - WebServices: FastEndpoints, Swagger, RateLimits (The new extension)
    builder.Services
        .AddInfrastructure(builder.Configuration)
        .AddWebServices(builder.Configuration);

    var app = builder.Build();

    // 4. Configure Pipeline (The "How")
    await app.ConfigurePipelineAsync();

    // 5. Startup Summary
    app.Lifetime.ApplicationStarted.Register(() =>
    {
        var addr = app.Urls.FirstOrDefault() ?? "http://localhost:5083";
        Log.Information("─────────────────────────────────────────────────────────");
        Log.Information("🎮 Game Auth API Online at {Address}", addr);
        Log.Information("⚡ Scalar Docs: {Address}/scalar/v1", addr);
        Log.Information("─────────────────────────────────────────────────────────");
    });

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "❌ Application terminated unexpectedly");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}

public partial class Program { }
