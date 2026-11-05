using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BuildWise.Lib.Users
{
    public interface ICurrentUserService
    {
        Task<Guid?> GetUserIdAsync();

        Task<Guid?> GetUserIdAsync(ClaimsPrincipal principal);

        Task<Guid?> EnsureUserExistsAsync();

        Task<Guid?> EnsureUserExistsAsync(ClaimsPrincipal principal);
    }
}
