using MediatR;
using Rest.Common;

namespace Rest.Features.Auth.Logout;

public record LogoutCommand(string UserId) : IRequest<Result>;