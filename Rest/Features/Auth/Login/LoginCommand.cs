using MediatR;
using Rest.Common;

namespace Rest.Features.Auth.Login;

public record LoginCommand(string Username, string Password) : IRequest<Result<AuthResponse>>;
