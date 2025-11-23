namespace Rest.Features.Auth;

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime Expiration,
    string Username,
    string Email
);
