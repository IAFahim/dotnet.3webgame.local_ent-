using Rest.Models;

namespace Rest.Services;

public interface ITokenService
{
    string GenerateJwtToken(ApplicationUser user);
}