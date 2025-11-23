using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Rest.Data;
using Rest.Models;
using Rest.Services;

namespace Rest.Features.Auth.Login;

public class LoginEndpoint(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ApplicationDbContext dbContext,
    ITokenService tokenService,
    TimeProvider timeProvider,
    ILogger<LoginEndpoint> logger)
    : Endpoint<LoginRequest, AuthResponse>
{
    public override void Configure()
    {
        Post("/api/v1/auth/login");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Login with username and password";
            s.Description = "Authenticate user and receive access and refresh tokens";
            s.Response<AuthResponse>(200, "Login successful with tokens");
            s.Response(401, "Invalid credentials or account locked");
        });
    }

    public override async Task HandleAsync(LoginRequest req, CancellationToken ct)
    {
        var user = await userManager.FindByNameAsync(req.Username);
        if (user is null)
        {
            logger.LogWarning("Invalid login attempt");
            ThrowError("Invalid credentials", 401);
        }

        var result = await signInManager.CheckPasswordSignInAsync(user, req.Password, true);
        if (!result.Succeeded)
        {
            if (result.IsLockedOut)
            {
                logger.LogWarning("User {Username} is locked out", req.Username);
                ThrowError("Account is locked out. Please try again later.", 401);
            }

            logger.LogWarning("Invalid login attempt");
            ThrowError("Invalid credentials", 401);
        }

        user = await userManager.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == user.Id, ct) ?? user;

        var accessToken = tokenService.GenerateJwtToken(user);
        var refreshToken = tokenService.GenerateRefreshToken();

        user.RefreshTokens.Add(refreshToken);

        var expiredTokens = user.RefreshTokens
            .Where(t => !t.IsActive && t.Created.AddDays(2) <= timeProvider.GetUtcNow().DateTime)
            .ToList();

        foreach (var token in expiredTokens)
        {
            user.RefreshTokens.Remove(token);
        }

        user.LastLoginAt = timeProvider.GetUtcNow().DateTime;

        dbContext.Entry(user).State = EntityState.Modified;
        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation("User {Username} logged in successfully", user.UserName);

        await SendAsync(new AuthResponse(
            accessToken,
            refreshToken.Token,
            refreshToken.Expires,
            user.UserName!,
            user.Email!
        ), cancellation: ct);
    }
}
