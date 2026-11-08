using System.Data;
using BuildWise.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BuildWise.Web.Services.CustomerPortal;

public sealed class CustomerAccountSettingsService(
    BuildWiseDbContext db,
    IPasswordHasher<CustomerAccount> passwordHasher)
{
    public async Task<CustomerProfileUpdateResult> UpdateProfileAsync(
        Guid accountId,
        string name,
        string email,
        string? phone,
        string address,
        int clientTypeId)
    {
        var normalizedEmail = email.Trim().ToUpperInvariant();
        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable)
            : null;

        var account = await db.CustomerAccounts.Include(item => item.Client).Include(item => item.User)
            .SingleOrDefaultAsync(item => item.Id == accountId && item.IsActive && item.Client.IsActive);
        if (account is null) return new CustomerProfileUpdateResult(null, false);

        var emailInUse = await db.CustomerAccounts.AnyAsync(item => item.Id != accountId && item.NormalizedEmail == normalizedEmail)
            || await db.Users.AnyAsync(item => item.Id != account.UserId && item.Email.ToUpper() == normalizedEmail);
        if (emailInUse) return new CustomerProfileUpdateResult(null, true);

        var emailChanged = !string.Equals(account.NormalizedEmail, normalizedEmail, StringComparison.Ordinal);
        account.Email = email.Trim();
        account.NormalizedEmail = normalizedEmail;
        account.Client.Name = name.Trim();
        account.Client.Email = account.Email;
        account.Client.Phone = Clean(phone);
        account.Client.Address = address.Trim();
        account.Client.ClientTypeId = clientTypeId;
        account.User.Name = account.Client.Name;
        account.User.Email = account.Email;
        account.User.Phone = account.Client.Phone;
        account.User.LastUpdated = DateTime.UtcNow;
        if (emailChanged)
        {
            account.SecurityStamp = Guid.NewGuid().ToString("N");
            account.User.SecurityStamp = account.SecurityStamp;
        }

        try
        {
            await db.SaveChangesAsync();
            if (transaction is not null) await transaction.CommitAsync();
            return new CustomerProfileUpdateResult(account, false);
        }
        catch (DbUpdateException exception) when (exception.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 2601 or 2627 })
        {
            if (transaction is not null) await transaction.RollbackAsync();
            db.ChangeTracker.Clear();
            return new CustomerProfileUpdateResult(null, true);
        }
    }

    public async Task<CustomerAccount?> ChangePasswordAsync(Guid accountId, string currentPassword, string newPassword)
    {
        var account = await db.CustomerAccounts.Include(item => item.Client).Include(item => item.User)
            .SingleOrDefaultAsync(item => item.Id == accountId && item.IsActive && item.Client.IsActive);
        if (account is null || string.IsNullOrWhiteSpace(account.PasswordHash)) return null;

        var verification = passwordHasher.VerifyHashedPassword(account, account.PasswordHash, currentPassword);
        if (verification == PasswordVerificationResult.Failed) return null;

        account.PasswordHash = passwordHasher.HashPassword(account, newPassword);
        account.SecurityStamp = Guid.NewGuid().ToString("N");
        account.User.SecurityStamp = account.SecurityStamp;
        account.User.LastUpdated = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return account;
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed record CustomerProfileUpdateResult(CustomerAccount? Account, bool EmailInUse);
