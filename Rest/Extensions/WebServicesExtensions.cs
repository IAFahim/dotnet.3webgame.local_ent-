using System.Threading.RateLimiting;
using FastEndpoints;
using FastEndpoints.Swagger;
using Rest.Middleware;

namespace Rest.Extensions;

public static class WebServicesExtensions
{
    public static IServiceCollection AddWebServices(this IServiceCollection services, IConfiguration config)
    {
        // 1. FastEndpoints & Swagger
        services.AddFastEndpoints();
        services.SwaggerDocument(o =>
        {
            o.DocumentSettings = s =>
            {
                s.Title = "Game Auth API";
                s.Version = "v1";
                s.Description = "High-performance Authentication & Wallet API";
            };
            o.EnableJWTBearerAuth = true;
            o.ShortSchemaNames = true;
            o.AutoTagPathSegmentIndex = 0;
        });

        // 2. Authorization (Crucial Fix from before)
        services.AddAuthorization();

        // 3. Exception Handling
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        // 4. Rate Limiting
        var rateLimitingEnabled = config.GetValue("RateLimiting:Enabled", true);
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            if (!rateLimitingEnabled)
            {
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

        // 5. Performance & Networking
        services.AddResponseCaching();
        services.AddResponseCompression(opts => opts.EnableForHttps = true);
        services.AddHealthChecks().AddDbContextCheck<Data.ApplicationDbContext>();
        services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy =>
                policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
        });

        return services;
    }
}
