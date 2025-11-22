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

        var user = new ApplicationUser { UserName = request.Username, Email = request.Email };
        var result = await userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            var error = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result.Failure<AuthResponse>(new Error("Auth.RegisterFailed", error));
        }

        var token = tokenService.GenerateJwtToken(user);
        
        // Note: Ideally use a Refresh Token flow here for "Perfect" security
        return new AuthResponse(token, DateTime.UtcNow.AddHours(2), user.UserName!, user.Email!);
    }
}