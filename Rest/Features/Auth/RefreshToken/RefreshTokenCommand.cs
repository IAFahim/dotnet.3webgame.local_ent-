using MediatR;
using Rest.Common;

namespace Rest.Features.Auth.RefreshToken;

public record RefreshTokenCommand(string AccessToken, string RefreshToken) : IRequest<Result<AuthResponse>>;
