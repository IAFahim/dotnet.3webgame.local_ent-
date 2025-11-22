using MediatR;
using Rest.Common;
using Rest.Models;

namespace Rest.Features.Auth.Login;

public record LoginCommand(string Username, string Password) : IRequest<Result<AuthResponse>>;