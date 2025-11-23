using FastEndpoints;
using FastEndpoints.Swagger;
using Scalar.AspNetCore;
using Serilog;
using Rest.Middleware;

namespace Rest.Extensions;

public static class PipelineExtensions
{
    public static async Task ConfigurePipelineAsync(this WebApplication app)
    {
        // 1. Security & Logging
        app.UseSecurityHeaders();
        app.UseExceptionHandler(); // This must use the handler we defined in Services
        app.UseSerilogRequestLogging();

        // 2. Development Tools
        if (app.Environment.IsDevelopment())
        {
            // app.UseDeveloperExceptionPage(); // Disable this to test your custom handler
            app.UseSwaggerGen();
            app.MapScalarApiReference(options =>
            {
                options
                    .WithTitle("Game Auth API")
                    .WithTheme(ScalarTheme.Kepler)
                    .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
                    .WithPreferredScheme("Bearer")
                    .WithOpenApiRoutePattern("/swagger/v1/swagger.json");
            });

            await app.SeedDatabaseAsync();
        }
        else
        {
            app.UseHsts();
        }

        // 3. Networking & Auth
        app.UseHttpsRedirection();
        app.UseCors("AllowAll");
        app.UseResponseCompression();
        app.UseResponseCaching();

        // RATE LIMITING: Check config to ensure tests aren't blocked
        // (Tests usually disable this via appsettings or Factory)
        app.UseRateLimiter();

        app.UseAuthentication();
        app.UseAuthorization(); // <--- CRITICAL: Must be before FastEndpoints

        // 4. Endpoints
        // FIX: Removed the RoutePrefix = "api" block.
        // Your endpoints already have "/api/..." in their definition.
        app.UseFastEndpoints();

        app.MapHealthChecks("/health").AllowAnonymous();
    }
}
