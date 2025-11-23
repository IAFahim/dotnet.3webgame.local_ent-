using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using Rest.Data;
using Rest.Data.Interceptors;
using Rest.Models;
using Rest.Options;
using Rest.Services;

namespace Rest.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config, IHostApplicationBuilder? hostBuilder = null)
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

        // Database - PostgreSQL with Aspire
        if (hostBuilder != null)
        {
            hostBuilder.AddNpgsqlDbContext<ApplicationDbContext>("DefaultConnection",
                configureSettings: null,
                configureDbContextOptions: options =>
                {
                    options.UseSnakeCaseNamingConvention()
                        .EnableSensitiveDataLogging(false)
                        .EnableDetailedErrors(false)
                        .ConfigureWarnings(warnings =>
                            warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
                });
        }
        else
        {
            // Fallback for tests or non-Aspire scenarios  
            var connectionString = config.GetConnectionString("DefaultConnection")
                                   ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            
            services.AddDbContext<ApplicationDbContext>((sp, options) =>
            {
                var interceptor = sp.GetRequiredService<AuditableEntityInterceptor>();
                options.UseNpgsql(connectionString)
                    .AddInterceptors(interceptor)
                    .UseSnakeCaseNamingConvention()
                    .EnableSensitiveDataLogging(false)
                    .EnableDetailedErrors(false)
                    .ConfigureWarnings(warnings =>
                        warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
            });
        }

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

        return services;
    }
}
