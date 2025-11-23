using System.ComponentModel;
namespace Rest.Models;

public record Dots(
    [property: DefaultValue("new_hero")] string Username,
    [property: DefaultValue("hero@game.com")] string Email,
    [property: DefaultValue("Password123!")] string Password
);

public record LoginRequest(
    [property: DefaultValue("player1")] string Username,
    [property: DefaultValue("Player123!")] string Password
);

public record ChangePasswordRequest(
    [property: DefaultValue("Player123!")] string CurrentPassword,
    [property: DefaultValue("NewPassword123!")] string NewPassword,
    [property: DefaultValue("NewPassword123!")] string ConfirmNewPassword
);

public record AuthResponse(
    string Token,
    DateTime Expiration,
    string Username,
    string Email
);