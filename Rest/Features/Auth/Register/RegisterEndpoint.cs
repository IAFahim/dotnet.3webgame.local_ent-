using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Rest.Data;
using Rest.Models;
using Rest.Services;

namespace Rest.Features.Auth.Register;

public class RegisterEndpoint(
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext dbContext,
    ITokenService tokenService,
    ILogger<RegisterEndpoint> logger)
    : Endpoint<RegisterRequest, AuthResponse>
{
    public override void Configure()
    {
        Post("/api/v1/auth/register");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Register a new user account";
            s.Description = "Creates a new user account with username, email and password";
            s.Response<AuthResponse>(200, "User registered successfully with tokens");
            s.Response(400, "Validation failed or email already exists");
        });
    }

    public override async Task HandleAsync(RegisterRequest req, CancellationToken ct)
    {
        var existingUser = await userManager.FindByEmailAsync(req.Email);
        if (existingUser is not null)
        {
            ThrowError("Email is already registered.", 400);
        }

        var user = new ApplicationUser { UserName = req.Username, Email = req.Email, EmailConfirmed = false };

        var result = await userManager.CreateAsync(user, req.Password);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            logger.LogWarning("Failed to register user {Username}: {Errors}", req.Username, errors);
            ThrowError(errors, 400);
        }

        var accessToken = tokenService.GenerateJwtToken(user);
        var refreshToken = tokenService.GenerateRefreshToken();

        user.RefreshTokens.Add(refreshToken);

        if (dbContext.Entry(user).State == EntityState.Detached)
        {
            dbContext.Users.Attach(user);
        }

        dbContext.Entry(user).State = EntityState.Modified;
        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation("User {Username} registered successfully", user.UserName);

        await SendAsync(new AuthResponse(
            accessToken,
            refreshToken.Token,
            refreshToken.Expires,
            user.UserName!,
            user.Email!
        ), cancellation: ct);
    }
}
