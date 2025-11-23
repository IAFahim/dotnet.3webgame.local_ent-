using MediatR;
using Rest.Common;

namespace Rest.Features.Auth.ChangePassword;

public record ChangePasswordCommand(
    string UserId,
    string CurrentPassword,
    string NewPassword,
    string ConfirmNewPassword
) : IRequest<Result>;
