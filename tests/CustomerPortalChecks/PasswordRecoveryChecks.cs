using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using BuildWise.Data;
using BuildWise.Web.Services.CustomerPortal;
using BuildWise.Web.Services.Email;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

internal static class PasswordRecoveryChecks
{
    public static async Task RunAsync()
    {
        await using var db = new BuildWiseDbContext(new DbContextOptionsBuilder<BuildWiseDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var user = new User { Id = Guid.NewGuid(), Email = "customer@example.test", PasswordHash = "unused" };
        var client = new Client { Id = Guid.NewGuid(), Name = "Customer", Email = user.Email, Address = "Site", IsActive = true, Creator = user };
        var account = new CustomerAccount
        {
            Id = Guid.NewGuid(), User = user, Client = client, Email = user.Email,
            NormalizedEmail = user.Email.ToUpperInvariant(), SecurityStamp = "original-stamp", IsActive = true
        };
        var hasher = new PasswordHasher<CustomerAccount>();
        account.PasswordHash = hasher.HashPassword(account, "OriginalPassword123");
        db.CustomerAccounts.Add(account);
        await db.SaveChangesAsync();
        var protection = new EphemeralDataProtectionProvider();
        var sender = new CapturingRecoverySender();
        var settings = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["Application:PublicBaseUrl"] = "https://buildwise.example.test" }).Build();
        var recovery = new CustomerPasswordRecoveryService(db, hasher, protection, sender, settings,
            NullLogger<CustomerPasswordRecoveryService>.Instance);
        await recovery.RequestAsync("missing@example.test");
        Check(sender.Calls == 0, "Unknown accounts must not send email.");
        await recovery.RequestAsync(" CUSTOMER@EXAMPLE.TEST ");
        Check(sender.Calls == 1 && sender.Recipient == account.Email && sender.CreatorId == user.Id,
            "Recovery must find the active account and email its registered address.");
        var link = WebUtility.HtmlDecode(Regex.Match(sender.Body!, "href=\"([^\"]+)\"").Groups[1].Value);
        var uri = new Uri(link);
        Check(uri.Host == "buildwise.example.test" && uri.AbsolutePath == "/CustomerPortal/Account/ResetPassword",
            "Reset links must use the configured public origin.");
        var token = QueryHelpers.ParseQuery(uri.Query)["token"].ToString();
        Check(await recovery.IsValidAsync(token) && !await recovery.IsValidAsync("invalid"), "Only a protected valid link can reset passwords.");
        var expired = protection.CreateProtector("BuildWise.CustomerPasswordReset.v1").Protect(JsonSerializer.Serialize(new
        { AccountId = account.Id, Stamp = account.SecurityStamp, Email = account.NormalizedEmail, Expires = DateTime.Now.AddMinutes(-1) }));
        Check(!await recovery.IsValidAsync(expired) && !await recovery.ResetAsync(expired, "NewPassword12345"), "Expired links must be rejected.");
        Check(!await recovery.ResetAsync(token, "short"), "Password policy must be enforced by the service too.");
        var stamp = account.SecurityStamp;
        Check(await recovery.ResetAsync(token, "NewPassword12345"), "A valid recovery link must update the password.");
        Check(hasher.VerifyHashedPassword(account, account.PasswordHash, "NewPassword12345") != PasswordVerificationResult.Failed
            && hasher.VerifyHashedPassword(account, account.PasswordHash, "OriginalPassword123") == PasswordVerificationResult.Failed
            && account.SecurityStamp != stamp && account.User.SecurityStamp == account.SecurityStamp,
            "Reset must replace the password hash and revoke existing customer sessions.");
        Check(!await recovery.IsValidAsync(token) && !await recovery.ResetAsync(token, "AnotherPassword123"), "Links must be single use.");
        account.IsActive = false;
        await db.SaveChangesAsync();
        await recovery.RequestAsync(account.Email);
        Check(sender.Calls == 1, "Inactive accounts must not receive recovery links.");
        account.IsActive = true;
        await db.SaveChangesAsync();
        sender.Fail = true;
        await recovery.RequestAsync(account.Email);
        // Delivery failure must not expose whether an account exists to the caller.
        Console.WriteLine("PASS: password recovery origin, active account isolation, expiry, tampering, password policy, single use, session revocation and safe email failures.");
    }

    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }

    private sealed class CapturingRecoverySender : IEmailSender
    {
        public int Calls { get; private set; }
        public string? Body { get; private set; }
        public string? Recipient { get; private set; }
        public Guid CreatorId { get; private set; }
        public bool Fail { get; set; }
        public Task<string> SendAsync(string recipient, string subject, string bodyHtml, Guid creatorId, CancellationToken cancellationToken = default)
        {
            Calls++; Body = bodyHtml; Recipient = recipient; CreatorId = creatorId;
            if (Fail) throw new InvalidOperationException("Test rejection");
            return Task.FromResult("fake-id");
        }
        public Task<string> SendQuotationAsync(string recipient, string subject, string bodyHtml, Guid creatorId,
            Guid revisionId, byte[] pdf, string fileName, Guid sendId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
