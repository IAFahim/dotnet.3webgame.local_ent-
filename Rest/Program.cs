using Rest.Extensions;
using Rest.Middleware;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Add Services
builder.Services.AddControllers();

// Database & Identity
builder.Services.AddDatabaseConfiguration(builder.Configuration);
builder.Services.AddIdentityConfiguration();

// Auth & JWT
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddCorsConfiguration();

// OpenAPI (Swagger) Configuration
builder.Services.AddOpenApiDocumentation(); // Custom extension method below

var app = builder.Build();

// 2. Configure Pipeline
// Exception Middleware must be near the top
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    // Generate the OpenAPI JSON
    app.MapOpenApi();
    
    // Serve Scalar UI
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("Game Auth API")
            .WithTheme(ScalarTheme.DeepSpace)
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

app.UseHttpsRedirection();

// CORS must be before Auth
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Seed Data
await app.SeedDatabaseAsync();

app.Run();