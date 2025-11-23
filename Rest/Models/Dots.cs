using System.ComponentModel;
namespace Rest.Models;
public record ChangePasswordRequest(
    [property: DefaultValue("Password123!")] string CurrentPassword,
    [property: DefaultValue("NewPassword123!")] string NewPassword,
    [property: DefaultValue("NewPassword123!")] string ConfirmNewPassword
);