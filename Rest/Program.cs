using System.IdentityModel.Tokens.Jwt;
using System.Threading.RateLimiting;
using DotNetEnv;
using Microsoft.AspNetCore.RateLimiting;
using Rest.Data;
using Rest.Extensions;
using Rest.Middleware;
using Scalar.AspNetCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Game Auth API...");

    Env.TraversePath().Load();

    JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

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
    builder.Services.AddProblemDetails(options =>
    {
        options.CustomizeProblemDetails = ctx =>
        {
            ctx.ProblemDetails.Instance = $"{ctx.HttpContext.Request.Method} {ctx.HttpContext.Request.Path}";
            ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier;
            ctx.ProblemDetails.Extensions["requestId"] =
                ctx.HttpContext.Request.Headers["X-Request-ID"].FirstOrDefault()
                ?? ctx.HttpContext.TraceIdentifier;
        };
    });

    builder.Services.AddInfrastructure(builder.Configuration);

    builder.Services.AddResponseCaching();
    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
    });

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

    builder.Services.AddHealthChecks()
        .AddDbContextCheck<ApplicationDbContext>();

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
            policy.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader());
    });

    var app = builder.Build();

    app.UseSecurityHeaders();
    app.UseExceptionHandler();
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value ?? "unknown");
            diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme ?? "unknown");
            diagnosticContext.Set("RemoteIP", httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");
            var userAgent = httpContext.Request.Headers.UserAgent.ToString();
            diagnosticContext.Set("UserAgent", string.IsNullOrEmpty(userAgent) ? "unknown" : userAgent);
        };
    });

    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            options.WithTitle("Game Auth API")
                .WithTheme(ScalarTheme.Kepler)
                .WithLayout(ScalarLayout.Modern)
                .WithDownloadButton(true)
                .WithPreferredScheme("Bearer")
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
    app.UseResponseCompression();
    app.UseResponseCaching();
    app.UseRateLimiter();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers()
        .RequireRateLimiting("api");

    app.MapHealthChecks("/health")
        .AllowAnonymous();

    app.Lifetime.ApplicationStarted.Register(() =>
    {
        var addresses = app.Urls.Any() ? app.Urls : new[] { "http://localhost:5000" };

        foreach (var address in addresses)
        {
            Log.Information("─────────────────────────────────────────────────────────");
            Log.Information("🚀 Game Auth API: {Address}", address);
            Log.Information("📚 API Documentation: {Address}/scalar/v1", address);
            Log.Information("📄 OpenAPI Spec: {Address}/openapi/v1.json", address);
            Log.Information("❤️  Health Check: {Address}/health", address);
            Log.Information("─────────────────────────────────────────────────────────");
        }
    });

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

// Make the implicit Program class public so test projects can access it
public partial class Program { }

