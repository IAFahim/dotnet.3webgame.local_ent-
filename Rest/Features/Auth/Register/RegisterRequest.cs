using System.ComponentModel;

namespace Rest.Features.Auth.Register;

public record RegisterRequest(
    [property: DefaultValue("new_hero")] string Username,
    [property: DefaultValue("hero@game.com")] string Email,
    [property: DefaultValue("Password123!")] string Password
);