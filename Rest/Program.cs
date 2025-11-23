using System.IdentityModel.Tokens.Jwt;
using System.Threading.RateLimiting;
using DotNetEnv;
using Microsoft.AspNetCore.RateLimiting;
using Rest.Data;
using Rest.Extensions;
using Rest.Middleware;
using Scalar.AspNetCore;
using Serilog;

// 1. Setup Serilog first
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Web Application...");
    
    Env.TraversePath().Load(); 
    
    // CRITICAL: Stop .NET from renaming "sub" to "http://.../nameidentifier"
    JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear(); 
    
    var builder = WebApplication.CreateBuilder(args);
    
    builder.Configuration.AddEnvironmentVariables();

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console());

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

    var app = builder.Build();

    app.UseExceptionHandler(); 
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            options.WithTitle("Game Auth API Docs")
                .WithTheme(ScalarTheme.Kepler)
                .WithLayout(ScalarLayout.Modern)
                .WithDownloadButton(true)
                .WithPreferredScheme("Bearer") // Prefills the auth scheme
                .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
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
    
    // Authentication MUST be before Authorization
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers(); 
    app.MapHealthChecks("/health");

    // 2. Register the URL logging callback
    app.Lifetime.ApplicationStarted.Register(() =>
    {
        var addresses = app.Urls;
        if (!addresses.Any()) addresses = new[] { "http://localhost:5083" }; // Fallback for dev default
        
        foreach (var address in addresses)
        {
            Log.Information("----------------------------------------------------------");
            Log.Information("🚀 Scalar API Docs: {Address}/scalar/v1", address);
            Log.Information("📄 Raw OpenAPI Spec: {Address}/openapi/v1.json", address);
            Log.Information("----------------------------------------------------------");
        }
    });

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}