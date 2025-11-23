using MediatR;
using Microsoft.AspNetCore.Identity;
using Rest.Common;
using Rest.Models;
using Rest.Services;

namespace Rest.Features.Auth.Register;

public sealed class RegisterCommandHandler(
    UserManager<ApplicationUser> userManager,
    ITokenService tokenService,
    ILogger<RegisterCommandHandler> logger) 
    : IRequestHandler<RegisterCommand, Result<AuthResponse>>
{
    public async Task<Result<AuthResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Registering user: {Username}", request.Username);

        var user = new ApplicationUser 
        { 
            UserName = request.Username, 
            Email = request.Email 
        };

        var result = await userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            var error = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result.Failure<AuthResponse>(new Error("Auth.RegisterFailed", error));
        }

        var accessToken = tokenService.GenerateJwtToken(user);
        var refreshToken = tokenService.GenerateRefreshToken();

        user.RefreshTokens.Add(refreshToken);
        await userManager.UpdateAsync(user);

        return new AuthResponse(
            accessToken, 
            refreshToken.Token, 
            refreshToken.Expires, 
            user.UserName!, 
            user.Email!
        );
    }
}