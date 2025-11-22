using System.Threading.Tasks;
using Rest.Common;
using Rest.Models;

namespace Rest.Services;

public interface IAuthService
{
    Task<Result<ApplicationUser>> RegisterAsync(RegisterRequest request);
    Task<Result<ApplicationUser>> LoginAsync(LoginRequest request);
    Task<Result> ChangePasswordAsync(string userId, ChangePasswordRequest request);
}