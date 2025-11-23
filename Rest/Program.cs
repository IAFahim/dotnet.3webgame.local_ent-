using System.Threading.RateLimiting;
using DotNetEnv;
using Microsoft.AspNetCore.RateLimiting;
using Rest.Data;
using Rest.Extensions;
using Rest.Middleware;
using Scalar.AspNetCore;
using Serilog;

// 1. Setup Serilog Bootstrap Logger
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Web Application...");

    Env.Load();

    var builder = WebApplication.CreateBuilder(args);

    // 2. Host Configuration: Use Serilog
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console());

    builder.Configuration.AddEnvironmentVariables();

    // 3. Add Services
    builder.Services.AddControllers();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();
    builder.Services.AddInfrastructure(builder.Configuration);

    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddFixedWindowLimiter("fixed", policy =>
        {
            policy.PermitLimit = 100;
            policy.Window = TimeSpan.FromMinutes(1);
            policy.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            policy.QueueLimit = 5;
        });
    });

    builder.Services.AddHealthChecks().AddDbContextCheck<ApplicationDbContext>();
    builder.Services.AddCors(o => o.AddPolicy("AllowAll", p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));
    builder.Services.AddOpenApiDocs();

    var app = builder.Build();

    // 4. Configure Pipeline
    app.UseExceptionHandler(); 
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi(); // Generates /openapi/v1.json
        
        // --- 🚀 ULTIMATE SCALAR CONFIGURATION ---
        app.MapScalarApiReference(options =>
        {
            options
                // 1. Visuals & Layout
                .WithTitle("Game Auth API Docs")
                .WithTheme(ScalarTheme.Kepler) // "Kepler" is a stunning high-contrast theme
                .WithLayout(ScalarLayout.Modern) // Sidebar layout for better navigation
                
                // 2. Interactivity
                .WithSearchHotKey("k") // Ctrl+K to search
                .WithDownloadButton(true) // Button to download openapi.json
                .WithDarkModeToggle(true) // Allow user to switch themes
                
                // 3. Authentication & Clients
                .WithPreferredScheme("Bearer") // Pre-selects JWT Auth
                .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient) // Default code snippet
                ;
        });
        
        await app.SeedDatabaseAsync();
    }
    else
    {
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseCors("AllowAll");
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers(); 
    app.MapHealthChecks("/health");

    // --- 5. DYNAMIC URL LOGGING ---
    // This fires ONLY after Kestrel has successfully bound to the port (e.g., 5083)
    app.Lifetime.ApplicationStarted.Register(() =>
    {
        var addresses = app.Urls;
        foreach (var address in addresses)
        {
            Log.Information("🚀 Scalar API Docs: {Address}/scalar/v1", address);
            Log.Information("📄 Raw OpenAPI Spec: {Address}/openapi/v1.json", address);
        }
    });

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}