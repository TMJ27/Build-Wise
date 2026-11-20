using System.Data;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using BuildWise.Data;
using BuildWise.Web.Services.Email;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace BuildWise.Web.Services.CustomerPortal;

public sealed class CustomerPasswordRecoveryService(
    BuildWiseDbContext db, IPasswordHasher<CustomerAccount> hasher, IDataProtectionProvider protection,
    IEmailSender sender, IConfiguration configuration, ILogger<CustomerPasswordRecoveryService> logger)
{
    private readonly IDataProtector protector = protection.CreateProtector("BuildWise.CustomerPasswordReset.v1");

    public async Task RequestAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToUpperInvariant();
        var account = await db.CustomerAccounts.AsNoTracking().Include(item => item.Client)
            .SingleOrDefaultAsync(item => item.NormalizedEmail == normalized && item.IsActive && item.Client.IsActive, cancellationToken);
        if (account is null) return;
        if (!Uri.TryCreate(configuration["Application:PublicBaseUrl"], UriKind.Absolute, out var origin)
            || origin.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(origin.UserInfo)
            || origin.Query.Length > 0 || origin.Fragment.Length > 0)
        {
            logger.LogWarning("Password recovery requires Application:PublicBaseUrl with an HTTPS application URL.");
            return;
        }
        var payload = new RecoveryToken(account.Id, account.SecurityStamp, account.NormalizedEmail, DateTime.Now.AddMinutes(30));
        var token = protector.Protect(JsonSerializer.Serialize(payload));
        var link = QueryHelpers.AddQueryString(origin.AbsoluteUri.TrimEnd('/') + "/CustomerPortal/Account/ResetPassword", "token", token);
        var body = $"<h2>Reset your BuildWise password</h2><p>Use the link below to choose a new password. It expires in 30 minutes and can only be used once.</p>"
            + $"<p><a href=\"{WebUtility.HtmlEncode(link)}\">Reset password</a></p><p>If you did not request this, you can ignore this email.</p>";
        try { await sender.SendPasswordResetAsync(account.Email, body, account.UserId, cancellationToken); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception)
        {
            // Never log the token, email body or raw provider exception.
            logger.LogWarning("Password recovery email failed with {ExceptionType}.", exception.GetType().Name);
        }
    }

    public async Task<bool> IsValidAsync(string? token)
    {
        var payload = Read(token);
        return payload is not null && await db.CustomerAccounts.AsNoTracking().AnyAsync(account =>
            account.Id == payload.AccountId && account.IsActive && account.Client.IsActive
            && account.SecurityStamp == payload.Stamp && account.NormalizedEmail == payload.Email);
    }

    public async Task<bool> ResetAsync(string? token, string password)
    {
        if (password.Length is < 12 or > 128) return false;
        var payload = Read(token);
        if (payload is null) return false;
        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable) : null;
        var account = await db.CustomerAccounts.Include(item => item.Client).Include(item => item.User)
            .SingleOrDefaultAsync(item => item.Id == payload.AccountId);
        if (account is null || !account.IsActive || !account.Client.IsActive
            || account.SecurityStamp != payload.Stamp || account.NormalizedEmail != payload.Email) return false;
        account.PasswordHash = hasher.HashPassword(account, password);
        account.SecurityStamp = Guid.NewGuid().ToString("N");
        account.User.SecurityStamp = account.SecurityStamp;
        account.User.LastUpdated = DateTime.Now;
        await db.SaveChangesAsync();
        if (transaction is not null) await transaction.CommitAsync();
        return true;
    }

    private RecoveryToken? Read(string? token)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 4096) return null;
        try
        {
            var payload = JsonSerializer.Deserialize<RecoveryToken>(protector.Unprotect(token));
            return payload is not null && payload.Expires > DateTime.Now ? payload : null;
        }
        catch (CryptographicException) { return null; }
        catch (JsonException) { return null; }
    }

    private sealed record RecoveryToken(Guid AccountId, string Stamp, string Email, DateTime Expires);
}
