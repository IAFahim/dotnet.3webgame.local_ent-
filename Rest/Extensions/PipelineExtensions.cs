using FastEndpoints;
using FastEndpoints.Swagger;
using Scalar.AspNetCore;
using Serilog;
using Rest.Middleware;
using Rest.Extensions; // For SeedDatabaseAsync

namespace Rest.Extensions;

public static class PipelineExtensions
{
    public static async Task ConfigurePipelineAsync(this WebApplication app)
    {
        // 1. Security & Logging
        app.UseSecurityHeaders();
        app.UseExceptionHandler();
        app.UseSerilogRequestLogging();

        // 2. Development Tools (Swagger/Scalar/Seeding)
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();

            // Generate JSON
            app.UseSwaggerGen();

            // Scalar UI
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
        app.UseRateLimiter();

        app.UseAuthentication();
        app.UseAuthorization();

        // 4. Endpoints
        app.UseFastEndpoints(c =>
        {
            c.Endpoints.RoutePrefix = "api";
        });

        app.MapHealthChecks("/health").AllowAnonymous();
    }
}
