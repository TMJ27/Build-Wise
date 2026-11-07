using System.Data;
using System.Security.Cryptography;
using System.ComponentModel.DataAnnotations;
using BuildWise.Data;
using BuildWise.Web.Areas.CustomerPortal.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BuildWise.Web.Services.CustomerPortal;

public sealed class CustomerRegistrationService(BuildWiseDbContext db, IPasswordHasher<CustomerAccount> hasher)
{
    public async Task<bool> RegisterAsync(CustomerRegistrationViewModel input)
    {
        if (!Validator.TryValidateObject(input, new ValidationContext(input), new List<ValidationResult>(), true)
            || string.IsNullOrWhiteSpace(input.Name) || string.IsNullOrWhiteSpace(input.Address)) return false;
        var email = input.Email.Trim();
        var normalized = email.ToUpperInvariant();
        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable) : null;
        // Never claim an existing customer profile by unverified email or reuse a staff identity.
        if (await db.CustomerAccounts.AnyAsync(account => account.NormalizedEmail == normalized)
            || await db.Users.AnyAsync(user => user.Email.ToUpper() == normalized)) return false;
        var now = DateTime.UtcNow;
        var auditUser = new User
        {
            Id = Guid.NewGuid(), Name = input.Name.Trim(), Email = email, Phone = Clean(input.Phone),
            CreationDate = now, IsActive = false, IsEmailConfirmed = false, GroupId = null,
            SecurityStamp = Guid.NewGuid().ToString("N")
        };
        auditUser.PasswordHash = new PasswordHasher<User>().HashPassword(auditUser,
            Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
        var client = new Client
        {
            Id = Guid.NewGuid(), Name = input.Name.Trim(), Email = email, Phone = Clean(input.Phone),
            Address = input.Address.Trim(), ClientTypeId = (int)input.ClientType,
            IsActive = true, CreationDate = now, Creator = auditUser
        };
        var account = new CustomerAccount
        {
            Id = Guid.NewGuid(), Client = client, User = auditUser, Email = email, NormalizedEmail = normalized,
            SecurityStamp = Guid.NewGuid().ToString("N"), IsActive = true, CreationDate = now
        };
        account.PasswordHash = hasher.HashPassword(account, input.Password);
        db.CustomerAccounts.Add(account);
        try
        {
            await db.SaveChangesAsync();
            if (transaction is not null) await transaction.CommitAsync();
            return true;
        }
        catch (DbUpdateException exception) when (exception.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 2601 or 2627 })
        {
            if (transaction is not null) await transaction.RollbackAsync();
            db.ChangeTracker.Clear();
            return false;
        }
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
