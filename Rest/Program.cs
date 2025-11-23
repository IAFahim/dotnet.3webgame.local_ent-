using System.IdentityModel.Tokens.Jwt;
using System.Threading.RateLimiting;
using DotNetEnv;
using Microsoft.AspNetCore.RateLimiting;
using Rest.Data;
using Rest.Extensions;
using Rest.Middleware;
using Scalar.AspNetCore;
using Serilog;

// 1. CLEAR MAPPING FIRST - CRITICAL FOR TESTS
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Game Auth API...");

    Env.TraversePath().Load();

    var builder = WebApplication.CreateBuilder(args);

    builder.Configuration.AddEnvironmentVariables();

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
    );

    builder.Services.AddControllers(options =>
    {
        options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = false;
    });

    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails(); // Simplified configuration

    builder.Services.AddInfrastructure(builder.Configuration);

    var rateLimitingEnabled = builder.Configuration.GetValue("RateLimiting:Enabled", true);

    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        if (!rateLimitingEnabled)
        {
            // Bypass limiter for tests
            options.AddPolicy("api", context => RateLimitPartition.GetNoLimiter("test"));
            return;
        }

        options.AddPolicy("api", context =>
        {
            var username = context.User.Identity?.Name ?? "anonymous";
            return RateLimitPartition.GetFixedWindowLimiter(username,
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 100,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 5
                });
        });
    });

    builder.Services.AddResponseCaching();
    builder.Services.AddResponseCompression(options => { options.EnableForHttps = true; });

    builder.Services.AddHealthChecks().AddDbContextCheck<ApplicationDbContext>();

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
    });

    var app = builder.Build();

    app.UseSecurityHeaders();
    app.UseExceptionHandler();
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
        app.MapOpenApi();
        app.MapScalarApiReference();
        await app.SeedDatabaseAsync(); // Only seed in Dev, not Testing
    }
    else
    {
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseCors("AllowAll");
    app.UseResponseCompression();
    app.UseResponseCaching();
    app.UseRateLimiter();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers().RequireRateLimiting("api");

    app.MapHealthChecks("/health").AllowAnonymous();

    // Removed the detailed startup log for brevity in this snippet

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}

public partial class Program { }
