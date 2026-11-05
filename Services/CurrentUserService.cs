using System.Security.Claims;
using System.Security.Cryptography;
using System.Data;
using BuildWise.Data;
using BuildWise.Lib.Rights;
using BuildWise.Lib.Users;
using BuildWise.Web.Services.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BuildWise.Web.Services;

public class CurrentUserService(
    IHttpContextAccessor httpContextAccessor,
    BuildWiseDbContext db,
    CognitoSettings settings) : ICurrentUserService
{
    public async Task<Guid?> GetUserIdAsync()
    {
        var principal = httpContextAccessor.HttpContext?.User;

        return principal is null ? null : await GetUserIdAsync(principal);
    }

    public async Task<Guid?> GetUserIdAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var identifier = principal.FindFirstValue("buildwise_user_id");

        if (Guid.TryParse(identifier, out var userId))
        {
            return await db.Users
                .AsNoTracking()
                .Where(user => user.Id == userId && user.IsActive)
                .Select(user => (Guid?)user.Id)
                .FirstOrDefaultAsync();
        }

        if (!bool.TryParse(principal.FindFirstValue("email_verified"), out var verified) || !verified)
        {
            return null;
        }

        var email = principal.FindFirstValue(ClaimTypes.Email)
            ?? principal.FindFirstValue("email");

        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        return await db.Users
            .AsNoTracking()
            .Where(user => user.Email == email && user.IsActive)
            .Select(user => (Guid?)user.Id)
            .FirstOrDefaultAsync();
    }

    public async Task<Guid?> EnsureUserExistsAsync()
    {
        var principal = httpContextAccessor.HttpContext?.User;
        if (principal is null)
        {
            return null;
        }

        var userId = await GetUserIdAsync(principal);
        return userId ?? await EnsureUserExistsAsync(principal, recordLogin: false);
    }

    public Task<Guid?> EnsureUserExistsAsync(ClaimsPrincipal principal)
    {
        return EnsureUserExistsAsync(principal, recordLogin: true);
    }

    private async Task<Guid?> EnsureUserExistsAsync(ClaimsPrincipal principal, bool recordLogin)
    {
        if (principal.Identity?.IsAuthenticated != true
            || !bool.TryParse(principal.FindFirstValue("email_verified"), out var verified)
            || !verified)
        {
            return null;
        }

        var email = (principal.FindFirstValue("email") ?? principal.FindFirstValue(ClaimTypes.Email))?.Trim();
        if (string.IsNullOrWhiteSpace(email) || email.Length > 50)
        {
            return null;
        }

        // Serialize first-time provisioning so concurrent sign-ins cannot create duplicate accounts.
        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable)
            : null;

        var normalizedEmail = email.ToLowerInvariant();
        // The first verified account claims a fresh deployment. An optional configured
        // email can restrict who is allowed to claim setup when provisioning a company.
        var bootstrap = (string.IsNullOrWhiteSpace(settings.BootstrapAdminEmail)
                || string.Equals(settings.BootstrapAdminEmail.Trim(), email, StringComparison.OrdinalIgnoreCase))
            && !await db.Users.AnyAsync(user => user.IsActive || user.ActivationDate != null || user.LastLoginDate != null)
            && !await db.UserGroups.AnyAsync(group => group.RightsId == (int)AccessRights.All);
        var existing = await db.Users.FirstOrDefaultAsync(user => user.Email.ToLower() == normalizedEmail);
        if (existing is not null)
        {
            if (!existing.IsActive && bootstrap)
            {
                SetUpAdministrator(existing);
                await db.SaveChangesAsync();
            }

            if (!existing.IsActive)
            {
                return null;
            }

            if (recordLogin)
            {
                existing.IsEmailConfirmed = true;
                existing.LastLoginDate = DateTime.UtcNow;
                await db.SaveChangesAsync();
            }

            if (transaction is not null)
            {
                await transaction.CommitAsync();
            }

            return existing.Id;
        }

        var now = DateTime.UtcNow;
        var name = principal.FindFirstValue("given_name") ?? principal.FindFirstValue("name") ?? email.Split('@')[0];
        var surname = principal.FindFirstValue("family_name");
        var newUser = new User
        {
            Id = Guid.NewGuid(),
            Name = name.Length > 50 ? name[..50] : name,
            Surname = surname?.Length > 50 ? surname[..50] : surname,
            Email = email,
            IsActive = bootstrap,
            IsEmailConfirmed = true,
            CreationDate = now,
            ActivationDate = bootstrap ? now : null,
            LastLoginDate = bootstrap ? now : null,
            SecurityStamp = Guid.NewGuid().ToString("N")
        };

        // Cognito owns the password. Store an unusable random local password hash for the required column.
        newUser.PasswordHash = new PasswordHasher<User>().HashPassword(newUser,
            Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));

        db.Users.Add(newUser);
        await db.SaveChangesAsync();

        if (bootstrap)
        {
            SetUpAdministrator(newUser);
            await db.SaveChangesAsync();
        }

        if (transaction is not null)
        {
            await transaction.CommitAsync();
        }

        return newUser.IsActive ? newUser.Id : null;
    }

    private void SetUpAdministrator(User user)
    {
        var now = DateTime.UtcNow;
        var administrators = new UserGroup
        {
            Id = Guid.NewGuid(),
            Name = "Administrators",
            RightsId = (int)AccessRights.All,
            CreatorId = user.Id,
            CreationDate = now
        };

        db.UserGroups.Add(administrators);
        user.Group = administrators;
        user.IsActive = true;
        user.IsEmailConfirmed = true;
        user.ActivationDate = now;
        user.LastLoginDate = now;
    }
}
