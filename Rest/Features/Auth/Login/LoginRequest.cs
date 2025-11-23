using System.ComponentModel;
namespace Rest.Features.Auth.Login;

public record LoginRequest(
    [property: DefaultValue("new_hero")] string Username,
    [property: DefaultValue("Password123!")] string Password
);