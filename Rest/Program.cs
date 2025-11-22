using System.Threading.RateLimiting;
using DotNetEnv;
using Microsoft.AspNetCore.RateLimiting;
using Rest.Data;
using Rest.Extensions;
using Rest.Middleware;
using Scalar.AspNetCore;

// 1. Load Env
Env.Load();

var builder = WebApplication.CreateBuilder(args);

// 2. Add Environment Variables
builder.Configuration.AddEnvironmentVariables();

// Add Services
builder.Services.AddControllers();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddInfrastructure(builder.Configuration);

// --- FIX 1: RATE LIMITER SERVICE DEFINITION ---
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    
    // This defines the policy named "fixed"
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

// Configure Pipeline
app.UseExceptionHandler(); 

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Game Auth API")
            .WithTheme(ScalarTheme.Mars)
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
    
    await app.SeedDatabaseAsync();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();

// --- FIX 2: MIDDLEWARE ORDER IS CRITICAL ---
app.UseCors("AllowAll");   // 1. CORS first
app.UseRateLimiter();      // 2. Rate Limiter second
app.UseAuthentication();   // 3. Auth third
app.UseAuthorization();

// --- FIX 3: REMOVE .RequireRateLimiting("fixed") HERE ---
// We will add it to the Controller instead.
app.MapControllers(); 
app.MapHealthChecks("/health");

app.Run();