using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Rest.Behaviors;
using Rest.Data;
using Rest.Data.Interceptors;
using Rest.Models;
using Rest.Options;
using Rest.Services;

namespace Rest.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        // Services
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<AuditableEntityInterceptor>();
        services.AddScoped<ITokenService, TokenService>();

        // JWT Configuration
        var jwtSection = config.GetSection(JwtSettings.SectionName);
        services.Configure<JwtSettings>(jwtSection);
        services.AddOptions<JwtSettings>()
            .Bind(jwtSection)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var jwtSettings = jwtSection.Get<JwtSettings>()
                          ?? throw new InvalidOperationException("JWT settings not configured.");

        // Database
        var connectionString = config.GetConnectionString("DefaultConnection")
                               ?? throw new InvalidOperationException(
                                   "Connection string 'DefaultConnection' not found.");

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            var interceptor = sp.GetRequiredService<AuditableEntityInterceptor>();
            options.UseSqlite(connectionString)
                .AddInterceptors(interceptor)
                .UseSnakeCaseNamingConvention()
                .EnableSensitiveDataLogging(false)
                .EnableDetailedErrors(false)
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
                .ConfigureWarnings(warnings =>
                    warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
        });

        // Identity
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequiredUniqueChars = 4;

                options.User.RequireUniqueEmail = true;

                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;

                options.SignIn.RequireConfirmedEmail = false;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        // Authentication
        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.SaveToken = true;
                // FIX: This stops .NET from renaming 'sub' to 'http://.../nameidentifier'
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings?.Issuer,
                    ValidAudience = jwtSettings?.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings?.Key!)),
                    ClockSkew = TimeSpan.Zero
                };
            });

        // OpenAPI Security Configuration (For Scalar/Swagger UI)
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info.Title = "Game Auth API";
                document.Info.Version = "v1";

                // 1. Define the Scheme
                var securityScheme = new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Description = "Enter JWT Bearer token **_only_**",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Reference = new OpenApiReference { Id = "Bearer", Type = ReferenceType.SecurityScheme }
                };

                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes.Add("Bearer", securityScheme);

                // 2. Apply the Requirement globally or per-path
                var securityRequirement = new OpenApiSecurityRequirement { { securityScheme, Array.Empty<string>() } };

                foreach (var path in document.Paths.Values)
                {
                    foreach (var operation in path.Operations.Values)
                    {
                        // Only add if the operation requires auth (you can filter here if needed)
                        // For now, we add it to all, allowing the user to click the lock icon.
                        operation.Security.Add(securityRequirement);
                    }
                }

                return Task.CompletedTask;
            });
        });

        // MediatR & Behaviors (FIX: This wires up validation)
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(ServiceExtensions).Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(typeof(ServiceExtensions).Assembly);

        return services;
    }
}
