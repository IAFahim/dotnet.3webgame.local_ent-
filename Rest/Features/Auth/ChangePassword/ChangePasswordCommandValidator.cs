using FluentValidation;

namespace Rest.Features.Auth.ChangePassword;

public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(8)
            .Matches("[A-Z]").WithMessage("Password must contain uppercase letter")
            .Matches("[0-9]").WithMessage("Password must contain number")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain special character")
            .NotEqual(x => x.CurrentPassword).WithMessage("New password cannot be the same as the old password.");

        RuleFor(x => x.ConfirmNewPassword)
            .Equal(x => x.NewPassword).WithMessage("Passwords do not match.");
    }
}
