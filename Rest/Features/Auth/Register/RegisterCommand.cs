using MediatR;
using Rest.Common;
using Rest.Models;

namespace Rest.Features.Auth.Register;

public record RegisterCommand(string Username, string Email, string Password) : IRequest<Result<AuthResponse>>;