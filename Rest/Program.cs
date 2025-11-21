using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Rest.Data;
using Rest.Models;
using Rest.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Configure Entity Framework with In-Memory Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseInMemoryDatabase("GameAuthDb"));

// Configure ASP.NET Core Identity - The Official .NET Way
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings - Strong by default
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
    options.Password.RequiredUniqueChars = 1;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false; // Set to true for email verification
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "SuperSecretKeyForDevelopment12345678901234567890";
var key = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // Set to true in production
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "GameAuthApi",
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "GameClient",
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// Configure JWT options using Options pattern
builder.Services.Configure<JwtOptions>(options =>
{
    options.Key = builder.Configuration["Jwt:Key"] ?? "SuperSecretKeyForDevelopment12345678901234567890";
    options.Issuer = builder.Configuration["Jwt:Issuer"] ?? "GameAuthApi";
    options.Audience = builder.Configuration["Jwt:Audience"] ?? "GameClient";
    options.ExpirationHours = 2;
});

// Register custom services with interfaces for testability
builder.Services.AddScoped<ITokenService, IdentityTokenService>();
builder.Services.AddScoped<IUserService, UserService>();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configure Swagger/OpenAPI with JWT support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Game Auth API - ASP.NET Core Identity",
        Version = "v1",
        Description = "Professional authentication using ASP.NET Core Identity with JWT"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Seed initial data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    
    // Ensure database is created
    await context.Database.EnsureCreatedAsync();
    
    // Seed test users
    if (!userManager.Users.Any())
    {
        var testUser1 = new ApplicationUser
        {
            UserName = "testuser",
            Email = "testuser@game.com",
            CoinBalance = 1000.00m,
            CreatedAt = DateTime.UtcNow,
            EmailConfirmed = true
        };
        await userManager.CreateAsync(testUser1, "Password123!");

        var testUser2 = new ApplicationUser
        {
            UserName = "player1",
            Email = "player1@game.com",
            CoinBalance = 500.50m,
            CreatedAt = DateTime.UtcNow,
            EmailConfirmed = true
        };
        await userManager.CreateAsync(testUser2, "Player123!");
        
        Console.WriteLine("✅ Test users created:");
        Console.WriteLine("   - testuser / Password123!");
        Console.WriteLine("   - player1 / Player123!");
    }
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Game Auth API V1");
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

// IMPORTANT: Order matters!
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

Console.WriteLine("🚀 Server starting...");
Console.WriteLine("📖 Swagger UI: http://localhost:5083/swagger");
Console.WriteLine("🔐 Using ASP.NET Core Identity");

app.Run();
