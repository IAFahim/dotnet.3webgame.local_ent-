using MediatR;
using Rest.Common;

namespace Rest.Features.Auth.Register;

public record RegisterCommand(string Username, string Email, string Password) : IRequest<Result<AuthResponse>>;
